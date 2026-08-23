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
  /// Set once the payload has been handed to an elevated instance. That instance now owns the
  /// scratch directory, so this one must leave it alone on exit.
  /// </summary>
  public bool HandedOffToElevatedInstance { get; set; }

  public HandoffState CaptureHandoff() => new() {
    PayloadRoot = PayloadRoot,
    ScratchDirectory = ScratchDirectory,
    LocalSourcePath = LocalSourcePath,
    DisplayVersion = DisplayVersion,
    AssetName = SelectedAsset?.Name,
    AssetUrl = SelectedAsset?.DownloadUrl,
    AssetSize = SelectedAsset?.SizeInBytes ?? 0,
    WebSite = Request.WebSite,
    VirtualPath = Request.VirtualPath,
    InstallDirectory = Request.InstallDirectory,
    Options = new Dictionary<string, string>(Request.Options, StringComparer.OrdinalIgnoreCase),
  };

  public void RestoreFrom(HandoffState handoff) {
    PayloadRoot = handoff.PayloadRoot;
    ScratchDirectory = handoff.ScratchDirectory;
    LocalSourcePath = handoff.LocalSourcePath;
    DisplayVersion = handoff.DisplayVersion;
    SelectedAsset = handoff.ToAsset();

    Request.SourceDirectory = handoff.PayloadRoot;
    Request.WebSite = handoff.WebSite;
    Request.VirtualPath = handoff.VirtualPath;
    Request.InstallDirectory = handoff.InstallDirectory;

    foreach (var option in handoff.Options) {
      Request.Options[option.Key] = option.Value;
    }
  }

  public void CleanUpScratch() {
    if (HandedOffToElevatedInstance) {
      return;
    }

    if (ScratchDirectory is { Length: > 0 }) {
      FileOperations.DeleteDirectoryIfExists(ScratchDirectory);
      ScratchDirectory = null;
    }
  }
}
