using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// The choices a user makes across the wizard pages before any of them are validated.
/// </summary>
public sealed class InstallRequest {
  /// <summary>
  /// Directory holding the extracted release contents (the directory containing setup.json).
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
      else if (FindReplaceableParentDirectory(InstallDirectory) is { } replaceableParent) {
        errors.Add($"Users other than administrators can rename or change the permissions of '{replaceableParent}', so they could replace RAWeb's files. "
          + @"Restrict that folder to administrators or choose a different directory (for example: C:\Program Files\RAWeb).");
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

  // rights that let someone rename a folder (or its children) or give themselves the right to do so
  private const FileSystemRights ReplaceRights = FileSystemRights.Delete
    | FileSystemRights.DeleteSubdirectoriesAndFiles
    | FileSystemRights.ChangePermissions
    | FileSystemRights.TakeOwnership;

  /// <summary>
  /// Returns the first existing folder between the drive root and <paramref name="installDirectory"/> that is
  /// owned by, or could be renamed by, someone other than an administrator.
  /// <br /><br />
  /// The installer secures the install directory itself, but renaming a parent folder moves the secured
  /// directory away. The user who renamed it could then put their own management service executable at
  /// the old path, which Windows would run as SYSTEM.
  /// </summary>
  private static string? FindReplaceableParentDirectory(string installDirectory) {
    var trustedSids = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) {
      new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Value,
      new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Value,
      "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464", // TrustedInstaller
      WindowsIdentity.GetCurrent().User?.Value,
    };

    var fullPath = Path.GetFullPath(installDirectory).TrimEnd(Path.DirectorySeparatorChar);
    var root = Path.GetPathRoot(fullPath);

    for (
      var directory = Path.GetDirectoryName(fullPath);
      // stop when we reach the root or when we reach a folder that does not exist
      directory is { Length: > 0 } && !string.Equals(directory, root, StringComparison.OrdinalIgnoreCase);
      // walk up the directory tree each iteration
      directory = Path.GetDirectoryName(directory)
    ) {
      if (!Directory.Exists(directory)) {
        // the installer will create missing folders with strict permissions,
        // so we should keep iterating up the path even if the folder does not exist yet
        continue;
      }

      var security = Directory.GetAccessControl(directory, AccessControlSections.Owner | AccessControlSections.Access);
      if (!trustedSids.Contains(security.GetOwner(typeof(SecurityIdentifier))?.Value)) {
        return directory;
      }

      foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))) {
        var appliesToFolder = !rule.PropagationFlags.HasFlag(PropagationFlags.InheritOnly);
        if (rule.AccessControlType == AccessControlType.Allow
          && appliesToFolder
          && !trustedSids.Contains(rule.IdentityReference.Value)
          && (rule.FileSystemRights & ReplaceRights) != 0) {
          return directory;
        }
      }
    }

    return null;
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
