using System.IO;

namespace RAWeb.Server.Installer.Setup;

public enum PlanWarningKind {
  UnsupportedWindows,
  Wsl2Missing,
  UpgradeInPlace,
  UpgradeRelocates,
  DirectoryInUseByAnotherInstall,
  DirectoryNotEmpty,
  SiteRootConflict,
  HealthCheckSkipped,
}

/// <summary>
/// Something the user must acknowledge before installing. <see cref="DefaultsToProceed"/> mirrors
/// whether setup.ps1 offered (Y/n) or (y/N) — the dangerous ones default to declining.
/// </summary>
public sealed record PlanWarning(PlanWarningKind Kind, string Title, string Detail, bool DefaultsToProceed);

/// <summary>
/// The fully resolved installation, ready to execute. Nothing here is read from the UI again.
/// </summary>
public sealed class InstallPlan {
  public required SetupManifest Manifest { get; init; }
  public required SystemState System { get; init; }
  public required InstallRequest Request { get; init; }

  public required string SourceRoot { get; init; }
  public required string InstallDirectory { get; init; }
  public required string VersionedDirectory { get; init; }
  public required string Version { get; init; }
  public required string WebSite { get; init; }
  public required string VirtualPath { get; init; }
  public required string ApplicationPoolName { get; init; }
  public required string ServiceName { get; init; }

  public required bool IsUpgrade { get; init; }
  public string? ExistingPhysicalPath { get; init; }
  public required bool RelocatesExistingInstall { get; init; }
  public required bool HasLegacyData { get; init; }
  public required string LegacyPath { get; init; }

  public required bool SiteAlreadyHasHttps { get; init; }
  public required bool WillEnableHttps { get; init; }
  public required bool WillCreateCertificate { get; init; }
  public required bool SkipHealthCheck { get; init; }

  public required IReadOnlyList<PlanWarning> Warnings { get; init; }

  public string MainExecutableSourcePath => Path.Combine(SourceRoot, Manifest.Layout.MainExecutable);
  public string ServiceExecutablePath => Path.Combine(VersionedDirectory, Manifest.Layout.ServiceExecutable);
  public string InstalledMainExecutablePath => Path.Combine(VersionedDirectory, Manifest.Layout.MainExecutable);
  public string AppDataDirectory => Path.Combine(VersionedDirectory, Manifest.Layout.AppDataDirectory);

  public string AppSettingsPath =>
    Path.Combine(VersionedDirectory, Manifest.Layout.AppSettingsFile.Replace('/', Path.DirectorySeparatorChar));

  public bool UsesHttps => SiteAlreadyHasHttps || WillEnableHttps;
}

public static class InstallPlanner {
  public const int HttpsPort = 443;

  public static InstallPlan Create(SetupManifest manifest, SystemState system, InstallRequest request, IisManager iis) {
    request.Normalize();

    var sourceRoot = Path.GetFullPath(Path.Combine(request.SourceDirectory, manifest.Layout.SourceRoot));
    var webSite = request.WebSite.Length > 0 ? request.WebSite : manifest.Defaults.WebSite;
    var virtualPath = request.VirtualPath.Length > 0 ? request.VirtualPath : manifest.Defaults.VirtualPath;

    var existingPhysicalPath = system.IsIisInstalled ? iis.GetApplicationPhysicalPath(webSite, virtualPath) : null;

    // Only a directory carrying appSettings.config is a real RAWeb installation. A bare folder at the
    // same path is not, and must not trigger the upgrade path.
    var existingAppSettingsPath = existingPhysicalPath is { Length: > 0 }
      ? Path.Combine(existingPhysicalPath, manifest.Layout.AppSettingsFile.Replace('/', Path.DirectorySeparatorChar))
      : null;
    var isUpgrade = existingAppSettingsPath is { Length: > 0 } && File.Exists(existingAppSettingsPath);

    request.ApplyDefaults(manifest, isUpgrade ? existingAppSettingsPath : null);

    var installDirectory = request.InstallDirectory.Length > 0
      ? request.InstallDirectory
      : ResolveInstallDirectory(manifest, webSite, virtualPath, existingPhysicalPath);

    var version = Naming.ReadVersion(Path.Combine(sourceRoot, manifest.Layout.MainExecutable));
    var versionedDirectory = Path.Combine(installDirectory, Naming.VersionedDirectoryName(version));

    var resolvedCurrentDirectory = ResolveInstallDirectory(manifest, webSite, virtualPath, existingPhysicalPath);
    var relocates = isUpgrade
      && !string.Equals(installDirectory.TrimEnd('\\'), resolvedCurrentDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
    var legacyPath = Path.Combine(systemDrive + Path.DirectorySeparatorChar, "inetpub", "RAWeb");
    var hasLegacyData = !isUpgrade && Directory.Exists(Path.Combine(legacyPath, "App_Data"));

    var httpsBinding = system.IsIisInstalled
      ? iis.GetBindings(webSite).FirstOrDefault(binding => binding.Protocol == "https" && binding.Port == HttpsPort)
      : null;
    var siteHasHttps = httpsBinding is not null;
    var siteHasCertificate = httpsBinding?.CertificateHash is { Length: > 0 };

    // An existing binding is left alone; a missing certificate on an existing binding is still filled in.
    var willEnableHttps = !siteHasHttps && request.GetBooleanOption("enableHttps", fallback: true);
    var willCreateCertificate = !siteHasCertificate
      && (willEnableHttps || siteHasHttps)
      && request.GetBooleanOption("createCertificate", fallback: true);

    var warnings = BuildWarnings(
      manifest, system, request, iis, webSite, virtualPath,
      installDirectory, existingPhysicalPath, isUpgrade, relocates);

    return new InstallPlan {
      Manifest = manifest,
      System = system,
      Request = request,
      SourceRoot = sourceRoot,
      InstallDirectory = installDirectory,
      VersionedDirectory = versionedDirectory,
      Version = version,
      WebSite = webSite,
      VirtualPath = virtualPath,
      ApplicationPoolName = Naming.AppPoolName(webSite, virtualPath),
      ServiceName = Naming.ServiceName(webSite, virtualPath),
      IsUpgrade = isUpgrade,
      ExistingPhysicalPath = existingPhysicalPath,
      RelocatesExistingInstall = relocates,
      HasLegacyData = hasLegacyData,
      LegacyPath = legacyPath,
      SiteAlreadyHasHttps = siteHasHttps,
      WillEnableHttps = willEnableHttps,
      WillCreateCertificate = willCreateCertificate,
      SkipHealthCheck = request.GetBooleanOption("skipHealthCheck"),
      Warnings = warnings,
    };
  }

  /// <summary>
  /// True only when the site and virtual path already carry a real RAWeb installation. A bare folder
  /// at the same path is not enough; it must contain an appSettings.config, ensuring that an empty application
  /// slot never gets mistaken for an upgrade.
  /// </summary>
  public static bool IsUpgrade(SetupManifest manifest, SystemState system, IisManager iis, string webSite, string virtualPath) {
    if (!system.IsIisInstalled) {
      return false;
    }

    var existingPhysicalPath = iis.GetApplicationPhysicalPath(webSite, virtualPath);
    if (existingPhysicalPath is not { Length: > 0 }) {
      return false;
    }

    var appSettingsPath = Path.Combine(
      existingPhysicalPath, manifest.Layout.AppSettingsFile.Replace('/', Path.DirectorySeparatorChar));
    return File.Exists(appSettingsPath);
  }

  /// <summary>
  /// Prefers the directory an existing installation already lives in so upgrades stay put, unless that
  /// directory is the legacy inetpub location.
  /// </summary>
  public static string ResolveInstallDirectory(SetupManifest manifest, string webSite, string virtualPath, string? existingPhysicalPath) {
    if (existingPhysicalPath is { Length: > 0 }) {
      var baseDirectory = Path.GetDirectoryName(existingPhysicalPath);
      if (baseDirectory is { Length: > 0 } && !FileOperations.IsUnderInetpub(baseDirectory)) {
        return baseDirectory;
      }
    }

    return Naming.DefaultInstallDirectory(manifest.Defaults.InstallDirBase, webSite, virtualPath);
  }

  private static IReadOnlyList<PlanWarning> BuildWarnings(
    SetupManifest manifest,
    SystemState system,
    InstallRequest request,
    IisManager iis,
    string webSite,
    string virtualPath,
    string installDirectory,
    string? existingPhysicalPath,
    bool isUpgrade,
    bool relocates) {
    var warnings = new List<PlanWarning>();

    if (!system.IsSupportedWindows) {
      warnings.Add(new PlanWarning(
        PlanWarningKind.UnsupportedWindows,
        "This edition of Windows is not supported",
        "RAWeb is intended for Windows 10, Windows 11, and Windows Server.",
        DefaultsToProceed: false
      ));
    }

    if (!system.IsWsl2Installed) {
      warnings.Add(new PlanWarning(
        PlanWarningKind.Wsl2Missing,
        "WSL2 is not installed",
        "The web client will be unavailable. See https://raweb.app/docs/wsl2-install.",
        DefaultsToProceed: !manifest.Requirements.RequiresWsl2
      ));
    }

    if (request.GetBooleanOption("skipHealthCheck")) {
      warnings.Add(new PlanWarning(
        PlanWarningKind.HealthCheckSkipped,
        "The post-install health check will be skipped",
        "The health check confirms RAWeb's server can start. Without it, a failed installation will not be detected.",
        DefaultsToProceed: true
      ));
    }

    if (isUpgrade) {
      warnings.Add(new PlanWarning(
        PlanWarningKind.UpgradeInPlace,
        $"RAWeb is already installed at '{webSite}/{virtualPath}'",
        "Continuing replaces the existing application. Resources, policies, and other app data are preserved.",
        DefaultsToProceed: true
      ));

      if (relocates) {
        warnings.Add(new PlanWarning(
          PlanWarningKind.UpgradeRelocates,
          "The installation directory is changing",
          $"The existing installation lives in '{Path.GetDirectoryName(existingPhysicalPath)}' but '{installDirectory}' was chosen. "
          + "The new directory will be checked for existing content to prevent accidental data loss.",
          DefaultsToProceed: false
        ));
      }
    }

    var conflictingApplications = Array.Empty<string>();
    if (!isUpgrade && system.IsIisInstalled && Directory.Exists(installDirectory)) {
      var normalized = installDirectory.TrimEnd('\\', '/');
      conflictingApplications = iis.GetApplications()
        .Where(application => {
          var physical = application.PhysicalPath.TrimEnd('\\', '/');
          return physical.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || physical.StartsWith(normalized + "\\", StringComparison.OrdinalIgnoreCase);
        })
        .Select(application => $"{application.SiteName}{application.Path}")
        .ToArray();

      if (conflictingApplications.Length > 0) {
        warnings.Add(new PlanWarning(
          PlanWarningKind.DirectoryInUseByAnotherInstall,
          $"'{installDirectory}' is already in use by another RAWeb installation",
          string.Join(Environment.NewLine, conflictingApplications)
          + Environment.NewLine + "Consider choosing a different installation directory.",
          DefaultsToProceed: false
        ));
      }
    }

    var upgradesInPlace = isUpgrade && !relocates;
    if (!upgradesInPlace
      && conflictingApplications.Length == 0
      && Directory.Exists(installDirectory)
      && Directory.EnumerateFileSystemEntries(installDirectory).Any()) {
      warnings.Add(new PlanWarning(
        PlanWarningKind.DirectoryNotEmpty,
        $"'{installDirectory}' is not empty",
        "Existing files will be overwritten during installation. Data may be permanently lost.",
        DefaultsToProceed: false
      ));
    }

    if (system.IsIisInstalled && iis.GetSitePhysicalPath(webSite) is { Length: > 0 } siteRoot) {
      var conflictingFolder = Path.Combine(siteRoot, virtualPath);
      if (Directory.Exists(conflictingFolder)) {
        warnings.Add(new PlanWarning(
          PlanWarningKind.SiteRootConflict,
          $"'{conflictingFolder}' may conflict with the installation",
          "A physical folder with the same name as the virtual path already exists under the site root. Consider removing it first.",
          DefaultsToProceed: true
        ));
      }
    }

    return warnings;
  }
}
