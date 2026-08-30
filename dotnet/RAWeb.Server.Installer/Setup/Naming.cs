using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Derives the names that identify one logical installation. Every name here is a function of
/// site + virtual path (never the install directory) so it stays stable across upgrades that
/// relocate the files.
/// </summary>
public static class Naming {
  public static string InstallHash(string value) {
    var key = value.ToLowerInvariant().TrimEnd('\\', '/');
    using var md5 = MD5.Create();
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
    var builder = new StringBuilder(hash.Length * 2);
    foreach (var b in hash) {
      builder.Append(b.ToString("x2"));
    }
    return builder.ToString();
  }

  private static string SiteKey(string site, string virtualPath) => $"{site}|{virtualPath.Trim('/', '\\')}".ToLowerInvariant();

  public static string AppPoolName(string site, string virtualPath) => "raweb-" + InstallHash(SiteKey(site, virtualPath));

  public static string ServiceName(string site, string virtualPath) => "RAWebMgmt-" + InstallHash(SiteKey(site, virtualPath));

  public static string UninstallRegistryKeyName(string site, string virtualPath) => "RAWeb-" + InstallHash(SiteKey(site, virtualPath));

  public static string DisplayName(string site, string virtualPath) {
    var trimmed = virtualPath.Trim('/', '\\');
    return string.IsNullOrWhiteSpace(trimmed) ? $"RAWeb ({site})" : $"RAWeb ({site}) ({trimmed})";
  }

  public static string ServiceDisplayName(string site, string virtualPath) =>
    "RAWeb Management" + DisplayName(site, virtualPath).Substring("RAWeb".Length);

  public static string VersionedDirectoryName(string version) => $"{version}__{DateTime.UtcNow:yyyyMMdd-HHmmss}";

  public static string DefaultInstallDirectory(string installDirBase, string site, string virtualPath) {
    var safeSite = ReplaceAny(site.Trim(), "<>:\"/\\|?*", '_');
    var safeVirtualPath = ReplaceAny(virtualPath.Trim('/', '\\').Replace('/', '\\'), "<>:\"|?*", '_');
    return string.IsNullOrWhiteSpace(safeVirtualPath)
      ? Path.Combine(installDirBase, safeSite)
      : Path.Combine(installDirBase, safeSite, safeVirtualPath);
  }

  /// <summary>
  /// Reads the product version from the payload's main executable.
  /// </summary>
  public static string ReadVersion(string executablePath) {
    if (!File.Exists(executablePath)) {
      return "0.0.0.0-missing";
    }

    try {
      var version = FileVersionInfo.GetVersionInfo(executablePath).FileVersion;
      return string.IsNullOrWhiteSpace(version) ? "0.0.0.0-missing" : version!;
    }
    catch (FileNotFoundException) {
      return "0.0.0.0-missing";
    }
  }

  private static string ReplaceAny(string value, string illegalCharacters, char replacement) {
    var builder = new StringBuilder(value.Length);
    foreach (var character in value) {
      builder.Append(illegalCharacters.IndexOf(character) >= 0 ? replacement : character);
    }
    return builder.ToString();
  }
}
