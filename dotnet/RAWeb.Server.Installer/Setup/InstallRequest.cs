using System.IO;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// The choices a user makes across the wizard pages before any of them are validated.
/// </summary>
public sealed class InstallRequest {
  /// <summary>
  /// Directory holding the extracted release payload (the directory containing setup.json).
  /// </summary>
  public string SourceDirectory { get; set; } = "";

  /// <summary>
  /// The web site under which the application will be hosted.
  /// </summary>
  public string WebSite { get; set; } = "";

  /// <summary>
  /// The virtual path under the IIS web site where the application will be hosted.
  /// </summary>
  public string VirtualPath { get; set; } = "";

  /// <summary>
  /// The directory where files should be placed.
  /// <br/><br/>
  /// Empty means "derive from the site, virtual path, and any existing installation".
  /// </summary>
  public string InstallDirectory { get; set; } = "";

  /// <summary>
  /// Values for the options declared in setup.json, keyed by option id.
  /// </summary>
  public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);

  public string? GetOption(string id) => Options.TryGetValue(id, out var value) ? value : null;

  public bool GetBooleanOption(string id, bool fallback = false) =>
    GetOption(id) is { } value ? string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) : fallback;

  /// <summary>
  /// Fills any option the user did not set from the manifest default or from the installation being upgraded.
  /// </summary>
  public void ApplyDefaults(SetupManifest manifest, string? existingAppSettingsPath) {
    foreach (var option in manifest.Options) {
      if (Options.ContainsKey(option.Id)) {
        continue;
      }

      // for an upgrade, try to carry forward the existing value from the appSettings.config file
      var carriedForward = option.AppSetting is { Length: > 0 } key && existingAppSettingsPath is { Length: > 0 }
        ? AppSettingsFile.TryRead(existingAppSettingsPath, key)
        : null;

      Options[option.Id] = carriedForward ?? option.DefaultValue;
    }
  }

  public void Normalize() {
    SourceDirectory = SourceDirectory.Trim().Trim('"');
    WebSite = WebSite.Trim();
    VirtualPath = VirtualPath.Trim().Trim('/', '\\', ' ');
    InstallDirectory = InstallDirectory.Trim().Trim('"', '/', '\\', ' ');
  }

  /// <summary>
  /// Returns the reasons this request cannot be installed.
  /// If the list is empty, the request is valid.
  /// </summary>
  public IReadOnlyList<string> Validate(SetupManifest manifest) {
    var errors = new List<string>();

    if (!Directory.Exists(SourceDirectory)) {
      errors.Add($"The release directory '{SourceDirectory}' does not exist.");
    }

    if (WebSite.Length == 0) {
      errors.Add("A web site must be selected.");
    }

    if (InstallDirectory.Length > 0) {
      if (FileOperations.IsUnderInetpub(InstallDirectory)) {
        errors.Add(@"Installation inside C:\inetpub is not allowed. Choose a different directory (for example C:\Program Files\RAWeb).");
      }
      else if (!IsRooted(InstallDirectory)) {
        errors.Add("The installation directory must be an absolute path.");
      }
    }

    foreach (var option in manifest.Options) {
      if (Options.TryGetValue(option.Id, out var value)
        && option.Choices.Count > 0
        && !option.Choices.Any(choice => string.Equals(choice.Value, value, StringComparison.OrdinalIgnoreCase))) {
        var allowed = string.Join(", ", option.Choices.Select(choice => choice.Value));
        errors.Add($"'{value}' is not a valid value for {option.Label}. Valid values are: {allowed}.");
      }
    }

    return errors;
  }

  private static bool IsRooted(string path) {
    try {
      return Path.IsPathRooted(path);
    }
    catch (ArgumentException) {
      return false;
    }
  }
}
