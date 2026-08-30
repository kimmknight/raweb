using System.IO;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard;

/// <summary>
/// Reports a step of the preparation process.
/// </summary>
public delegate void ProgressReporter(string status, string detail, double? percent);

public sealed class SynchronousProgress<T>(Action<T> callback) : IProgress<T> {
  public void Report(T value) => callback(value);
}

public static class ReleasePreparer {
  /// <summary>
  /// Downloads and extracts the release archive if needed and returns the path to the extracted
  /// release distributables.
  /// <br/><br/>
  /// This method returns null if the release is only a PowerShell script.
  /// </summary>
  public static string? Prepare(WizardState state, ProgressReporter report, CancellationToken token) {
    if (state.LocalSourcePath is { Length: > 0 } local && Directory.Exists(local)) {
      report("Reading files", Path.GetFileName(local), null);
      return ReleaseSource.ResolvePayloadRoot(local);
    }

    state.ScratchDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(state.ScratchDirectory);

    if (state.PreviewBranch is { Length: > 0 } branch) {
      return PreparePreview(state, state.PreviewOwner!, branch, report, token);
    }

    var archivePath = state.LocalSourcePath;

    if (state.SelectedAsset is { } asset) {
      archivePath = Path.Combine(state.ScratchDirectory, asset.Name);
      report("Downloading", asset.Name, 0);

      ReleaseSource.Download(
        asset,
        archivePath,
        new SynchronousProgress<double>(percent => report("Downloading", asset.Name, percent)),
        token
      );
    }

    if (archivePath is null || !File.Exists(archivePath)) {
      throw new InstallFailedException("No release archive was available to install.");
    }

    var extractedDirectory = Path.Combine(state.ScratchDirectory, "extracted");
    report("Extracting", Path.GetFileName(archivePath), 0);

    ReleaseSource.Extract(
      archivePath,
      extractedDirectory,
      new SynchronousProgress<double>(percent => report("Extracting", Path.GetFileName(archivePath), percent)),
      token
    );

    return ReleaseSource.ResolvePayloadRoot(extractedDirectory);
  }

  /// <summary>
  /// Fetches whatever install.raweb.app currently serves for this branch.
  /// <br/><br/>
  /// This method returns the extracted preview release contents when it is a real archive
  /// (a build artifact carrying setup.json or a source archive carrying only setup.ps1
  /// or setup.json).
  /// <br/><br/>
  /// This method returns null when it is only a PowerShell script.
  /// </summary>
  private static string? PreparePreview(WizardState state, string owner, string branch, ProgressReporter report, CancellationToken token) {
    report("Checking install.raweb.app", $"{owner}/{branch}", null);

    var response = ReleaseSource.FetchPreviewInstall(
      owner, branch,
      new SynchronousProgress<double>(percent => report("Downloading", $"{owner}/{branch}", percent)),
      token);

    if (response.Kind == ReleaseSource.PreviewResponseKind.Script) {
      return null;
    }

    var archivePath = Path.Combine(state.ScratchDirectory!, $"{branch}.zip");
    File.WriteAllBytes(archivePath, response.Content);

    var extractedDirectory = Path.Combine(state.ScratchDirectory!, "extracted");
    report("Extracting", Path.GetFileName(archivePath), 0);

    ReleaseSource.Extract(
      archivePath,
      extractedDirectory,
      new SynchronousProgress<double>(percent => report("Extracting", Path.GetFileName(archivePath), percent)),
      token
    );
    File.Delete(archivePath);

    // if the extracted contents only has a single ZIP file, extract that ZIP file into a subfolder and then delete the ZIP file
    var extractedContents = Directory.GetFileSystemEntries(extractedDirectory);
    if (extractedContents.Length == 1 && Path.GetExtension(extractedContents[0]).Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
      var innerZipPath = extractedContents[0];
      var innerExtractedDirectory = Path.Combine(extractedDirectory, "inner-extracted");
      report("Extracting", Path.GetFileName(innerZipPath), 0);

      ReleaseSource.Extract(
        innerZipPath,
        innerExtractedDirectory,
        new SynchronousProgress<double>(percent => report("Extracting", Path.GetFileName(innerZipPath), percent)),
        token
      );

      File.Delete(innerZipPath);
      extractedDirectory = innerExtractedDirectory;
    }

    return ReleaseSource.ResolvePayloadRoot(extractedDirectory);
  }

  /// <summary>
  /// Loads the manifest and checks the system for prerequisites. If necessary,
  /// the source code will also be built before continuing to the rest of
  /// the installation process.
  /// </summary>
  public static void FinishPreparation(WizardState state, string payloadRoot, ProgressReporter report, Action<LogEntry>? onBuildLog, CancellationToken token) {
    state.Strategy = ReleaseInspector.DetermineStrategy(payloadRoot);

    report("Reading setup.json", "", null);
    state.Manifest = SetupManifest.Load(Path.Combine(payloadRoot, "setup.json"));

    if (SourceBuilder.NeedsBuild(payloadRoot, state.Manifest)) {
      if (onBuildLog is not null) {
        state.Log.Logged += onBuildLog;
      }
      try {
        SourceBuilder.Build(payloadRoot, state.Log, token);
      }
      finally {
        if (onBuildLog is not null) {
          state.Log.Logged -= onBuildLog;
        }
      }
    }

    report("Checking system prerequisites", "This can take a moment.", null);
    state.System = SystemChecks.Inspect(state.Manifest!, state.Log);

    state.Request.SourceDirectory = payloadRoot;
    state.DisplayVersion = Naming.ReadVersion(
      Path.Combine(
        payloadRoot,
        state.Manifest.Layout.SourceRoot,
        state.Manifest.Layout.MainExecutable
      )
    );
  }
}
