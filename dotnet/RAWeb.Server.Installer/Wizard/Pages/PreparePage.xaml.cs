using System.IO;
using iNKORE.UI.WPF.Modern.Media.Animation;
using RAWeb.Server.Installer.Setup;
using RAWeb.Server.Installer.Wizard.Controls;

namespace RAWeb.Server.Installer.Wizard.Pages;

/// <summary>
/// Downloads the chosen archive if needed, unpacks it, reads setup.json, and runs the prerequisite
/// sweep. Advances on its own once everything is ready. No elevation happens here: the UAC prompt is
/// deferred until the user has finished choosing options.
/// <br/><br/>
/// The status view (progress bar/status/detail) and the build log view live in a nested Frame so a
/// source build can drill into a full-page log and drill back out when it is done.
/// </summary>
public partial class PreparePage : WizardPage {
  private readonly BuildStatusView _statusView = new();
  private readonly BuildLogView _logView = new();

  private CancellationTokenSource? _cancellation;
  private bool _isRunning;
  private bool _completed;
  private bool _buildingFromSource;

  public PreparePage() {
    InitializeComponent();
    ContentFrame.Navigate(_statusView);
  }

  public override string Title => _buildingFromSource ? "Building from source" : "Getting the files ready";

  public override string? Description => _buildingFromSource
    ? "This archive contains source code and needs to be built. This may take several minutes."
    : "Downloading and unpacking the release.";

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
        _statusView.SetStatus("Ready", "");
        _statusView.SetProgress(100);
      }

      return;
    }

    CanGoNext = false;
    _statusView.ClearError();
    _ = PrepareAsync();
  }

  private async Task PrepareAsync() {
    _isRunning = true;
    RaiseNavigationStateChanged();

    _cancellation = new CancellationTokenSource();
    var token = _cancellation.Token;

    try {
      var payloadRoot = await Task.Run(() => ReleasePreparer.Prepare(State, Report, token), token);

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
      _statusView.SetStatus("Cancelled", "");
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
  /// Loads the setup.json manifest and checks the system for prerequisites.
  /// </summary>
  private async Task FinishPreparationAsync(string payloadRoot, CancellationToken token) {
    if (SourceBuilder.NeedsBuild(payloadRoot, SetupManifest.Load(Path.Combine(payloadRoot, "setup.json")))) {
      _logView.Clear();
      _buildingFromSource = true;
      RaiseNavigationStateChanged();
      ContentFrame.Navigate(_logView, new DrillInNavigationTransitionInfo());
    }

    try {
      await Task.Run(
        () => ReleasePreparer.FinishPreparation(State, payloadRoot, Report, OnBuildLogged, token),
        token
      );
    }
    finally {
      if (_buildingFromSource) {
        _buildingFromSource = false;
        RaiseNavigationStateChanged();
        if (ContentFrame.CanGoBack) {
          ContentFrame.GoBack();
        }
      }
    }

    Complete();
  }

  private void Complete() {
    _completed = true;
    _statusView.SetStatus("Ready", "");
    RaiseRequestNext();
  }

  private void ShowError(string title, string message, string status) {
    _statusView.ShowError(title, message);
    _statusView.SetStatus(status.Length > 0 ? status : "Something went wrong", "");
    _statusView.SetProgress(0);
  }

  private void Report(string status, string detail, double? percent) =>
    Dispatcher.BeginInvoke(new Action(() => {
      _statusView.SetStatus(status, detail);
      _statusView.SetProgress(percent);
    }));

  private void OnBuildLogged(LogEntry entry) =>
    Dispatcher.BeginInvoke(new Action(() => _logView.AddEntry(entry)));
}
