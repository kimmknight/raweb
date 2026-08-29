using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard;

/// <summary>
/// Everything the wizard accumulates as the user moves forward. Pages read and write this; nothing
/// under <c>Setup/</c> knows it exists.
/// </summary>
public sealed class WizardState {
  public InstallLog Log { get; } = new();
  public IisManager Iis { get; } = new();

  /// <summary>
  /// The GitHub asset to download, or null when installing from a local path.
  /// </summary>
  public ReleaseAsset? SelectedAsset { get; set; }

  /// <summary>
  /// A folder or .zip the user picked, or null when downloading from GitHub.
  /// </summary>
  public string? LocalSourcePath { get; set; }

  /// <summary>
  /// Set when an unreleased branch was picked instead of a release or a local path. PreparePage
  /// skips straight to <see cref="InstallStrategy.RemotePreview"/> when this is set, since there is
  /// nothing for this app to download itself.
  /// </summary>
  public string? PreviewOwner { get; set; }

  public string? PreviewBranch { get; set; }

  /// <summary>
  /// Directory the payload was unpacked into, deleted on exit.
  /// </summary>
  public string? ScratchDirectory { get; set; }

  /// <summary>
  /// Directory holding the prepared payload, i.e. the one containing setup.json or setup.ps1.
  /// </summary>
  public string PayloadRoot { get; set; } = "";

  public InstallStrategy Strategy { get; set; } = InstallStrategy.Unsupported;
  public SetupManifest? Manifest { get; set; }
  public SystemState? System { get; set; }
  public InstallRequest Request { get; } = new();
  public InstallPlan? Plan { get; set; }
  public InstallResult? Result { get; set; }

  public string DisplayVersion { get; set; } = "";

  /// <summary>
  /// When true, the installer will advance past a page automatically once it has
  /// a valid default or command-line value (see StartupOptions)
  /// <br/><br/>
  /// It does not answer the overwrite-warning confirmation on its own,
  /// so you must combine it with <see cref="Overwrite"/> for an installer
  /// that runs without prompting.
  /// </summary>
  public bool Express { get; set; }

  /// <summary>
  /// When true, the app will automatically anwer "Continue" for warnings that
  /// would otherwise require confirmation to avoid overwriting files.
  /// </summary>
  public bool Overwrite { get; set; }

  /// <summary>
  /// Used to specify whether and when the window should close after installation
  /// completion or failure.
  /// </summary>
  public AutoCloseMode AutoClose { get; set; } = AutoCloseMode.Never;

  /// <summary>
  /// When true, the welcome page will be skipped.
  /// <br/><br/>
  /// If the installer was not launched as an administrator, it will automatically
  /// relaunch as an administrator since the welcome page typically handles
  /// messaging around elevation.
  /// </summary>
  public bool NoWelcome { get; set; }

  /// <summary>
  /// The process's original, unparsed command-line arguments. WelcomePage passes these straight back
  /// to <see cref="ElevationHelper.RelaunchElevated(string)"/> for in case it needs to relaunch the
  /// installer in an elevated process.
  /// </summary>
  public string[] RawArguments { get; set; } = [];

  /// <summary>
  /// Parsed from "--source" (see <see cref="StartupOptions.Source"/>). When set, VersionPage resolves
  /// it and advances automatically instead of showing the picker.
  /// </summary>
  public SourceSelection? Source { get; set; }

  /// <summary>
  /// Set instead of <see cref="Source"/> when "--source" failed to parse.
  /// </summary>
  public string? SourceError { get; set; }

  /// <summary>
  /// From "--release-label" (see <see cref="StartupOptions.ReleaseLabel"/>). Overrides the version
  /// label <see cref="Source"/> would otherwise display.
  /// </summary>
  public string? ReleaseLabel { get; set; }

  public void CleanUpScratch() {
    if (ScratchDirectory is { Length: > 0 }) {
      FileOperations.DeleteDirectoryIfExists(ScratchDirectory);
      ScratchDirectory = null;
    }
  }
}
