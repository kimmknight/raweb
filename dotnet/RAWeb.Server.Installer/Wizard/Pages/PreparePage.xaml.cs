using System.IO;
using System.Windows;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

/// <summary>
/// Downloads the chosen archive if needed, unpacks it, reads setup.json, and runs the prerequisite
/// sweep. Advances on its own once everything is ready. No elevation happens here: the UAC prompt is
/// deferred until the user has finished choosing options.
/// </summary>
public partial class PreparePage : WizardPage {
  private CancellationTokenSource? _cancellation;
  private bool _isRunning;
  private bool _completed;

  public PreparePage() => InitializeComponent();

  public override string Title => "Getting the files ready";

  public override string Description => "Downloading and unpacking the release.";

  public override bool ShowBack => !_isRunning;

  public override bool IsBusy => _isRunning;

  public override void OnEnter(WizardNavigationDirection direction) {
    if (_completed) {
      if (direction == WizardNavigationDirection.Forward) {
        RaiseRequestNext();
      }
      // When the download is already complete, but the user stepped back to this page,
      // we don't need to re-download the release.
      else {
        CanGoNext = true;
        SetStatus("Ready", "");
        Progress.IsIndeterminate = false;
        Progress.Value = 100;
      }

      return;
    }

    CanGoNext = false;
    ErrorBar.IsOpen = false;
    _ = PrepareAsync();
  }

  private async Task PrepareAsync() {
    _isRunning = true;
    RaiseNavigationStateChanged();

    _cancellation = new CancellationTokenSource();
    var token = _cancellation.Token;

    try {
      var payloadRoot = await Task.Run(() => Prepare(token), token);

      // if we only have a PowerShell script, there is no need to inspect a ZIP file for proper contents
      if (payloadRoot is null) {
        State.Strategy = InstallStrategy.RemotePreview;
        Complete();
        return;
      }

      State.PayloadRoot = payloadRoot;
      State.Strategy = ReleaseInspector.DetermineStrategy(payloadRoot);

      if (State.Strategy == InstallStrategy.Unsupported) {
        throw new InstallFailedException("The selected release contains neither setup.json nor setup.ps1, so it cannot be installed.");
      }

      // setup.ps1 does preparation for itself, so we don't need to do anything else here.
      if (State.Strategy == InstallStrategy.LegacyPowerShell) {
        Complete();
        return;
      }

      await FinishPreparationAsync(payloadRoot, token);
    }
    catch (OperationCanceledException) {
      SetStatus("Cancelled", "");
    }
    catch (Exception exception) {
      ShowError(
        "Could not prepare the release",
        exception.Message,
        "Go back and choose a different release."
      );
    }
    finally {
      _isRunning = false;
      RaiseNavigationStateChanged();
    }
  }

  /// <summary>
  /// Loads the manifest and checks the system for prerequisites.
  /// </summary>
  private async Task FinishPreparationAsync(string payloadRoot, CancellationToken token) {
    State.Strategy = ReleaseInspector.DetermineStrategy(payloadRoot);

    Report("Reading setup.json", "", null);
    State.Manifest = SetupManifest.Load(Path.Combine(payloadRoot, "setup.json"));

    Report("Checking system prerequisites", "This can take a moment.", null);
    State.System = await Task.Run(() => SystemChecks.Inspect(State.Manifest!, State.Log), token);

    State.Request.SourceDirectory = payloadRoot;
    State.DisplayVersion = Naming.ReadVersion(
      Path.Combine(
        payloadRoot,
        State.Manifest.Layout.SourceRoot,
        State.Manifest.Layout.MainExecutable
      )
    );

    Complete();
  }

  private void Complete() {
    _completed = true;
    SetStatus("Ready", "");
    RaiseRequestNext();
  }

  /// <summary>
  /// Downloads and extracts the release archive if needed, and returns the path to the extracted payload root.
  /// Returns null if the release is only a PowerShell script, which will be handled by <see cref="LegacySetupPage"/>.
  /// </summary>
  private string? Prepare(CancellationToken token) {
    if (State.LocalSourcePath is { Length: > 0 } local && Directory.Exists(local)) {
      Report("Reading files", Path.GetFileName(local), null);
      return ReleaseSource.ResolvePayloadRoot(local);
    }

    State.ScratchDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(State.ScratchDirectory);

    if (State.PreviewBranch is { Length: > 0 } branch) {
      return PreparePreview(State.PreviewOwner!, branch, token);
    }

    var archivePath = State.LocalSourcePath;

    if (State.SelectedAsset is { } asset) {
      archivePath = Path.Combine(State.ScratchDirectory, asset.Name);
      Report("Downloading", asset.Name, 0);

      ReleaseSource.Download(
        asset,
        archivePath,
        new Progress<double>(percent => Report("Downloading", asset.Name, percent)),
        token
      );
    }

    if (archivePath is null || !File.Exists(archivePath)) {
      throw new InstallFailedException("No release archive was available to install.");
    }

    var extractedDirectory = Path.Combine(State.ScratchDirectory, "extracted");
    Report("Extracting", Path.GetFileName(archivePath), 0);

    ReleaseSource.Extract(
      archivePath,
      extractedDirectory,
      new Progress<double>(percent => Report("Extracting", Path.GetFileName(archivePath), percent)),
      token
    );

    return ReleaseSource.ResolvePayloadRoot(extractedDirectory);
  }

  /// <summary>
  /// Fetches whatever install.raweb.app currently serves for this branch. Returns the extracted
  /// payload root when it is a real archive (a build artifact carrying setup.json, or a source
  /// archive carrying only setup.ps1, both installed exactly like a normal release from here on).
  /// Returns null when it is still only the PowerShell installer script that service has always
  /// returned historically.
  /// </summary>
  private string? PreparePreview(string owner, string branch, CancellationToken token) {
    Report("Checking install.raweb.app", $"{owner}/{branch}", null);

    var response = ReleaseSource.FetchPreviewInstall(
      owner, branch,
      new Progress<double>(percent => Report("Downloading", $"{owner}/{branch}", percent)),
      token);

    // The script itself embeds a download URL that expires after about a minute, so this copy is
    // discarded rather than saved. LegacySetupPage re-fetches a fresh one right before running it.
    if (response.Kind == ReleaseSource.PreviewResponseKind.Script) {
      return null;
    }

    var archivePath = Path.Combine(State.ScratchDirectory!, $"{branch}.zip");
    File.WriteAllBytes(archivePath, response.Content);

    var extractedDirectory = Path.Combine(State.ScratchDirectory!, "extracted");
    Report("Extracting", Path.GetFileName(archivePath), 0);

    ReleaseSource.Extract(
      archivePath,
      extractedDirectory,
      new Progress<double>(percent => Report("Extracting", Path.GetFileName(archivePath), percent)),
      token
    );
    File.Delete(archivePath);

    // if the extracted contents only has a single ZIP file, extract that ZIP file into a subfolder and then delete the ZIP file
    var extractedContents = Directory.GetFileSystemEntries(extractedDirectory);
    if (extractedContents.Length == 1 && Path.GetExtension(extractedContents[0]).Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
      var innerZipPath = extractedContents[0];
      var innerExtractedDirectory = Path.Combine(extractedDirectory, "inner-extracted");
      Report("Extracting", Path.GetFileName(innerZipPath), 0);

      ReleaseSource.Extract(
        innerZipPath,
        innerExtractedDirectory,
        new Progress<double>(percent => Report("Extracting", Path.GetFileName(innerZipPath), percent)),
        token
      );

      File.Delete(innerZipPath);
      extractedDirectory = innerExtractedDirectory;
    }

    return ReleaseSource.ResolvePayloadRoot(extractedDirectory);
  }

  private void ShowError(string title, string message, string status) {
    ErrorBar.Title = title;
    ErrorBar.Message = message;
    ErrorBar.IsOpen = true;

    SetStatus(status.Length > 0 ? status : "Something went wrong", "");
    Progress.IsIndeterminate = false;
    Progress.Value = 0;
  }

  private void Report(string status, string detail, double? percent) =>
    Dispatcher.BeginInvoke(new Action(() => {
      SetStatus(status, detail);

      if (percent is { } value) {
        Progress.IsIndeterminate = false;
        Progress.Value = value;
      }
      else {
        Progress.IsIndeterminate = true;
      }
    }));

  private void SetStatus(string status, string detail) {
    StatusText.Text = status;
    DetailText.Text = detail;
    DetailText.Visibility = detail.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
  }
}
