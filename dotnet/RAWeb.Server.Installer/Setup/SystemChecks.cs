using System.IO;
using System.Management;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RAWeb.Server.Installer.Setup;

public sealed class SystemState {
  public string OperatingSystemCaption { get; init; } = "";
  public string OperatingSystemVersion { get; init; } = "";
  public bool IsAdministrator { get; init; }
  public bool IsServer { get; init; }
  public bool IsHomeEdition { get; init; }
  public bool IsSupportedWindows { get; init; }
  public bool IsWsl2Installed { get; init; }
  public bool IsIisInstalled { get; init; }
  public bool AreIisFeaturesInstalled { get; init; }
  public IReadOnlyList<string> MissingIisFeatures { get; init; } = Array.Empty<string>();
  public Version? HostingBundleVersion { get; init; }
  public bool IsHostingBundleSatisfied { get; init; }
}

/// <summary>
/// Describes the host operating system without needing a manifest, for the welcome page.
/// </summary>
public sealed record OperatingSystemInfo(string Caption, string Version, bool IsServer, bool IsHome) {
  public bool IsSupported => !IsHome && (IsServer || Version.StartsWith("10.", StringComparison.Ordinal));
}

public static class SystemChecks {
  public const string Wsl2Path = @"C:\Program Files\WSL\wsl.exe";

  public static OperatingSystemInfo DescribeOperatingSystem() {
    var (caption, version) = ReadOperatingSystem();
    return new OperatingSystemInfo(
      caption.Length > 0 ? caption : "Windows",
      version,
      caption.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0,
      caption.IndexOf("Home", StringComparison.OrdinalIgnoreCase) >= 0
    );
  }

  public static SystemState Inspect(SetupManifest manifest, InstallLog log) {
    var (caption, version) = ReadOperatingSystem();
    var isServer = caption.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0;

    bool isIisInstalled;
    IReadOnlyList<string> missingFeatures;
    if (isServer) {
      missingFeatures = GetMissingServerFeatures(manifest.Requirements.ServerFeatures, out isIisInstalled, log);
    }
    else {
      missingFeatures = GetMissingClientFeatures(manifest.Requirements.ClientFeatures, out isIisInstalled, log);
    }

    var minimumBundle = Version.Parse(manifest.Requirements.HostingBundleMinimumVersion);
    var installedBundle = FindHostingBundleVersion();

    return new SystemState {
      OperatingSystemCaption = caption,
      OperatingSystemVersion = version,
      IsAdministrator = IsRunningAsAdministrator(),
      IsServer = isServer,
      IsHomeEdition = caption.IndexOf("Home", StringComparison.OrdinalIgnoreCase) >= 0,
      IsSupportedWindows = isServer || version.StartsWith("10.", StringComparison.Ordinal),
      IsWsl2Installed = File.Exists(Wsl2Path),
      IsIisInstalled = isIisInstalled,
      AreIisFeaturesInstalled = isIisInstalled && missingFeatures.Count == 0,
      MissingIisFeatures = missingFeatures,
      HostingBundleVersion = installedBundle,
      IsHostingBundleSatisfied = installedBundle is not null && installedBundle >= minimumBundle,
    };
  }

  public static bool IsRunningAsAdministrator() {
    using var identity = WindowsIdentity.GetCurrent();
    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
  }

  private static (string Caption, string Version) ReadOperatingSystem() {
    using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
    foreach (var entry in searcher.Get()) {
      using var os = (ManagementObject)entry;
      return (Convert.ToString(os["Caption"]) ?? "", Convert.ToString(os["Version"]) ?? "");
    }
    return ("", Environment.OSVersion.Version.ToString());
  }

  /// <summary>
  /// Queries the installed state of the given Windows Server features. Returns the list of missing
  /// features, and sets the out parameter to true when IIS is installed.
  /// </summary>
  private static IReadOnlyList<string> GetMissingServerFeatures(IReadOnlyList<string> features, out bool isIisInstalled, InstallLog log) {
    isIisInstalled = false;
    if (features.Count == 0) {
      return [];
    }

    var nameList = string.Join(",", features.Select(feature => "'" + feature.Replace("'", "''") + "'"));
    var result = ProcessRunner.RunPowerShell(
      $"Import-Module ServerManager -ErrorAction SilentlyContinue; " +
      $"Get-WindowsFeature -Name @({nameList}) | ForEach-Object {{ \"$($_.Name)=$($_.Installed)\" }}");

    if (!result.Succeeded) {
      log.Warning("Could not query Windows Server features; assuming none are installed.");
      return [.. features];
    }

    var states = result.OutputLines
      .Select(line => line.Split('='))
      .Where(parts => parts.Length == 2)
      .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);

    isIisInstalled = states.TryGetValue("Web-Server", out var webServer)
      && bool.TryParse(webServer, out var installed) && installed;

    return features
      .Where(feature => !(states.TryGetValue(feature, out var state) && bool.TryParse(state, out var on) && on))
      .ToArray();
  }

  /// <summary>
  /// Queries the installed state of the given Windows client features. Returns the list of missing
  /// features, and sets the out parameter to true when IIS is installed.
  /// </summary>
  private static IReadOnlyList<string> GetMissingClientFeatures(IReadOnlyList<string> features, out bool isIisInstalled, InstallLog log) {
    isIisInstalled = false;
    if (features.Count == 0) {
      return [];
    }

    var dism = ProcessRunner.Run(DismPath, "/online /get-features /format:table /english");
    if (!dism.Succeeded) {
      log.Warning("Could not query Windows optional features; assuming none are installed.");
      return [.. features];
    }

    bool IsEnabled(string feature) =>
      Regex.IsMatch(dism.StandardOutput, $@"^{Regex.Escape(feature)}\s*\|[^|]*Enabled", RegexOptions.Multiline
    );

    isIisInstalled = IsEnabled("IIS-WebServerRole");
    return features.Where(feature => !IsEnabled(feature)).ToArray();
  }

  public static string DismPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.System), "dism.exe");

  /// <summary>
  /// Finds the newest installed ASP.NET Core Hosting Bundle, which supplies the ASP.NET Core Module.
  /// </summary>
  public static Version? FindHostingBundleVersion() {
    string[] uninstallRoots =
    [
      @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
      @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    ];

    foreach (var root in uninstallRoots) {
      using var key = Registry.LocalMachine.OpenSubKey(root);
      if (key is null) {
        continue;
      }

      Version? newest = null;
      foreach (var subKeyName in key.GetSubKeyNames()) {
        using var subKey = key.OpenSubKey(subKeyName);

        if (
          subKey?.GetValue("DisplayName") is not string displayName ||
          displayName.IndexOf("Hosting Bundle", StringComparison.OrdinalIgnoreCase) < 0 ||
          subKey?.GetValue("DisplayVersion") is not string displayVersion ||
          !displayVersion.StartsWith("10.", StringComparison.Ordinal) ||
          !Version.TryParse(displayVersion, out var parsed)
        ) {
          continue;
        }

        if (newest is null || parsed > newest) {
          newest = parsed;
        }
      }

      if (newest is not null) {
        return newest;
      }
    }

    return null;
  }
}
