using System.Diagnostics;
using System.IO;
using System.Xml;

namespace RAWeb.Server.Installer.Setup;

public sealed record InstallResult(
  bool Succeeded,
  bool RestartRequired,
  TimeSpan Duration,
  string? WebInterfaceUrl,
  string? WorkspaceUrl,
  string? UninstallScriptPath,
  string? FailureMessage,
  bool RolledBack = false);

/// <summary>
/// Performs the installation described by an <see cref="InstallPlan"/>.
/// Every mutating step registers its undo step to the ollback stack before it runs.
/// </summary>
public sealed class InstallEngine(InstallLog log) {
  private const int TotalSteps = 13;
  private const string LegacyServiceName = "RAWebManagementService";

  private readonly IisManager _iis = new();

  public InstallResult Run(InstallPlan plan, CancellationToken cancellationToken) {
    var stopwatch = Stopwatch.StartNew();
    var rollback = new RollbackStack(log);

    try {
      if (InstallIisFeatures(plan, cancellationToken) is { RestartRequired: true }) {
        return new InstallResult(false, true, stopwatch.Elapsed, null, null, null,
          "A restart is required to complete IIS installation. Run the installer again after restarting.");
      }

      InstallHostingBundle(plan, cancellationToken);
      VerifyPayload(plan);
      CopyFiles(plan, rollback, cancellationToken);
      MigrateApplicationData(plan, cancellationToken);
      StopExistingServices(plan, rollback);
      ConfigureApplicationPool(plan, rollback);
      ConfigurePermissions(plan);
      ConfigureIisApplication(plan, rollback);
      StartServices(plan, rollback, cancellationToken);
      VerifyInstallation(plan, cancellationToken);
      CleanUp(plan);
      var uninstallScriptPath = RegisterInstallation(plan, rollback);

      rollback.Commit();
      stopwatch.Stop();

      var (webInterfaceUrl, workspaceUrl) = BuildUrls(plan);
      log.Success($"RAWeb installation succeeded in {Format(stopwatch.Elapsed)}.");

      return new InstallResult(true, false, stopwatch.Elapsed, webInterfaceUrl, workspaceUrl, uninstallScriptPath, null);
    }
    catch (OperationCanceledException) {
      log.Warning("Installation cancelled. Rolling back...");
      var rolledBack = rollback.Unwind();
      stopwatch.Stop();
      return new InstallResult(false, false, stopwatch.Elapsed, null, null, null, "Installation was cancelled.", rolledBack);
    }
    catch (Exception exception) {
      log.Error("INSTALLATION FAILED");
      log.Error(exception.Message);
      log.Error(exception.Source);
      log.Error(exception.StackTrace);
      var rolledBack = rollback.Unwind();
      stopwatch.Stop();
      return new InstallResult(false, false, stopwatch.Elapsed, null, null, null, exception.Message, rolledBack);
    }
  }

  // [1] ─────────────────────────────────────────────────────────────────────
  private FeatureInstallResult InstallIisFeatures(InstallPlan plan, CancellationToken cancellationToken) {
    log.Step(1, TotalSteps, "Installing IIS features...");
    cancellationToken.ThrowIfCancellationRequested();

    if (plan.System.AreIisFeaturesInstalled) {
      log.Detail("All required IIS features are already installed; skipping.");
      return new FeatureInstallResult(false);
    }

    if (plan.System.IsIisInstalled) {
      log.Detail("IIS is installed but some features are missing.");
    }

    log.Indeterminate("Installing IIS features");
    return WindowsFeatures.Enable(plan.System.MissingIisFeatures, plan.System.IsServer, log);
  }

  // [2] ─────────────────────────────────────────────────────────────────────
  private void InstallHostingBundle(InstallPlan plan, CancellationToken cancellationToken) {
    log.Step(2, TotalSteps, "Installing ASP.NET Core Module...");
    cancellationToken.ThrowIfCancellationRequested();

    if (plan.System.IsHostingBundleSatisfied) {
      log.Detail($"ASP.NET Core Module {plan.System.HostingBundleVersion} already installed; skipping.");
      return;
    }

    if (plan.System.HostingBundleVersion is { } installed) {
      log.Detail($"Older ASP.NET Core Hosting Bundle {installed} detected. "
        + $"The required minimum version is {plan.Manifest.Requirements.HostingBundleMinimumVersion}.");
    }

    HostingBundle.Install(plan.Manifest, log, cancellationToken);
  }

  // [3] ─────────────────────────────────────────────────────────────────────
  private void VerifyPayload(InstallPlan plan) {
    log.Step(3, TotalSteps, "Verifying release payload...");

    foreach (var relativePath in new[] { plan.Manifest.Layout.MainExecutable, plan.Manifest.Layout.ServiceExecutable }) {
      var path = Path.Combine(plan.SourceRoot, relativePath);
      if (!File.Exists(path)) {
        throw new InstallFailedException($"The release is missing '{relativePath}'. Expected it at {path}.");
      }
    }

    // TODO: if the payload is source code instead of pre-built binaries, we should build it here and fail if the build fails.

    log.Detail($"RAWeb {plan.Version}");
  }

  // [4] ─────────────────────────────────────────────────────────────────────
  private void CopyFiles(InstallPlan plan, RollbackStack rollback, CancellationToken cancellationToken) {
    log.Step(4, TotalSteps, "Copying files...");

    Directory.CreateDirectory(plan.VersionedDirectory);
    rollback.Push("Removing files...", () => {
      FileOperations.DeleteDirectoryIfExists(plan.VersionedDirectory);
      FileOperations.RemoveEmptyAncestorDirectories(Path.GetDirectoryName(plan.VersionedDirectory)!);
    });

    log.Detail(plan.VersionedDirectory);
    FileOperations.CopyDirectory(plan.SourceRoot, plan.VersionedDirectory, cancellationToken);
  }

  // [5] ─────────────────────────────────────────────────────────────────────
  private void MigrateApplicationData(InstallPlan plan, CancellationToken cancellationToken) {
    log.Step(5, TotalSteps, "Migrating application data...");

    Directory.CreateDirectory(plan.AppDataDirectory);

    if (plan.IsUpgrade && plan.ExistingPhysicalPath is { Length: > 0 } && Directory.Exists(plan.ExistingPhysicalPath)) {
      var previousAppData = Path.Combine(plan.ExistingPhysicalPath, plan.Manifest.Layout.AppDataDirectory);
      if (Directory.Exists(previousAppData)) {
        log.Detail($"From previous version: {plan.ExistingPhysicalPath}");
        FileOperations.CopyDirectory(previousAppData, plan.AppDataDirectory, cancellationToken);
      }
      else {
        log.Detail("No App_Data found in previous version; skipping.");
      }

      return;
    }

    if (plan.HasLegacyData) {
      MigrateLegacyData(plan, cancellationToken);
      return;
    }

    log.Detail("No previous data to migrate.");
  }

  private void MigrateLegacyData(InstallPlan plan, CancellationToken cancellationToken) {
    log.Detail($"From legacy installation: {plan.LegacyPath}");

    FileOperations.CopyDirectory(Path.Combine(plan.LegacyPath, "App_Data"), plan.AppDataDirectory, cancellationToken);

    foreach (var subdirectory in new[] { "resources", "multiuser-resources" }) {
      var source = Path.Combine(plan.LegacyPath, subdirectory);
      if (Directory.Exists(source)) {
        FileOperations.CopyDirectory(source, Path.Combine(plan.AppDataDirectory, subdirectory), cancellationToken);
      }
    }

    // pre-App_Data versions kept their settings in Web.config, se we need to move them into appSettings.config.
    var legacyWebConfig = Path.Combine(plan.LegacyPath, "Web.config");
    if (!File.Exists(legacyWebConfig) || File.Exists(plan.AppSettingsPath)) {
      return;
    }

    try {
      var document = new XmlDocument();
      document.Load(legacyWebConfig);

      if (document.SelectSingleNode("/configuration/appSettings") is XmlElement appSettings && appSettings.ChildNodes.Count > 0) {
        File.WriteAllText(plan.AppSettingsPath, "<?xml version=\"1.0\"?>\n" + appSettings.OuterXml);
      }
    }
    catch (XmlException exception) {
      log.Warning($"Could not extract appSettings from the legacy Web.config: {exception.Message}");
    }
  }

  // [6] ─────────────────────────────────────────────────────────────────────
  private void StopExistingServices(InstallPlan plan, RollbackStack rollback) {
    log.Step(6, TotalSteps, "Stopping existing services...");

    _iis.StopApplicationPool(plan.ApplicationPoolName);

    if (!plan.IsUpgrade) {
      return;
    }

    ServiceManager.StopSafe(plan.ServiceName, log);
    ServiceManager.UnregisterSafe(LegacyServiceName, log);

    var previousPhysicalPath = plan.ExistingPhysicalPath;
    var previousPool = _iis.GetApplication(plan.WebSite, plan.VirtualPath)?.ApplicationPool;
    var serviceDisplayName = Naming.ServiceDisplayName(plan.WebSite, plan.VirtualPath);

    rollback.Push("Reverting IIS physical path and app pool to the previous version...", () => {
      _iis.UpdateApplication(plan.WebSite, plan.VirtualPath, previousPhysicalPath, previousPool);

      if (previousPhysicalPath is { Length: > 0 } && Directory.Exists(previousPhysicalPath)) {
        ServiceManager.UnregisterSafe(plan.ServiceName, log);

        var previousServiceExecutable = Path.Combine(previousPhysicalPath, plan.Manifest.Layout.ServiceExecutable);
        if (!File.Exists(previousServiceExecutable)) {
          previousServiceExecutable = Path.Combine(previousPhysicalPath, "bin", "RAWeb.Server.Management.ServiceHost.exe");
        }

        ServiceManager.Register(previousServiceExecutable, plan.ServiceName, previousPool ?? plan.ApplicationPoolName, serviceDisplayName, log);
        ServiceManager.Start(plan.ServiceName, log);
      }

      FileOperations.DeleteDirectoryIfExists(plan.VersionedDirectory);
      FileOperations.RemoveEmptyAncestorDirectories(Path.GetDirectoryName(plan.VersionedDirectory)!);
    });
  }

  // [7] ─────────────────────────────────────────────────────────────────────
  private void ConfigureApplicationPool(InstallPlan plan, RollbackStack rollback) {
    log.Step(7, TotalSteps, "Configuring application pool...");

    if (_iis.EnsureApplicationPool(plan.ApplicationPoolName)) {
      rollback.Push("Removing application pool...", () => _iis.RemoveApplicationPool(plan.ApplicationPoolName));
      log.Detail($"Created: {plan.ApplicationPoolName}");
    }
    else {
      log.Detail($"Using existing: {plan.ApplicationPoolName}");
    }

    // Windows only registers the "IIS AppPool\<name>" virtual account once the pool has started at
    // least once, and step 8 needs to resolve that account to set ACLs.
    _iis.StartApplicationPool(plan.ApplicationPoolName);
    _iis.StopApplicationPool(plan.ApplicationPoolName);
  }

  // [8] ─────────────────────────────────────────────────────────────────────
  private void ConfigurePermissions(InstallPlan plan) {
    log.Step(8, TotalSteps, "Configuring permissions...");

    PermissionsManager.Apply(
      plan.VersionedDirectory,
      plan.AppDataDirectory,
      plan.InstalledMainExecutablePath,
      plan.ApplicationPoolName);
  }

  // [9] ─────────────────────────────────────────────────────────────────────
  private void ConfigureIisApplication(InstallPlan plan, RollbackStack rollback) {
    log.Step(9, TotalSteps, "Configuring IIS application...");

    try {
      _iis.EnableWebSockets(plan.WebSite);
    }
    catch (Exception exception) {
      log.Warning($"Could not enable WebSockets on '{plan.WebSite}': {exception.Message}");
    }

    if (plan.IsUpgrade) {
      _iis.UpdateApplication(plan.WebSite, plan.VirtualPath, plan.VersionedDirectory, plan.ApplicationPoolName);
      log.Detail($"Updated physical path -> {plan.VersionedDirectory}");
    }
    else {
      _iis.CreateApplication(plan.WebSite, plan.VirtualPath, plan.VersionedDirectory, plan.ApplicationPoolName);
      log.Detail($"Created application '{plan.WebSite}/{plan.VirtualPath}' -> {plan.VersionedDirectory}");
      rollback.Push("Removing IIS application...", () => _iis.RemoveApplication(plan.WebSite, plan.VirtualPath));
    }

    _iis.ConfigureAuthentication(plan.WebSite, plan.VirtualPath);

    // Strip Negotiate and NTLM at the application root so IIS stops attaching WWW-Authenticate headers
    // to its own 401 responses, which would pre-empt RAWeb's own sign-in flow.
    var removedProviders = _iis.RemoveWindowsAuthenticationProviders(plan.WebSite, plan.VirtualPath, ["Negotiate", "NTLM"]);
    if (removedProviders.Count > 0) {
      rollback.Push("Restoring Windows authentication providers...",
        () => _iis.AddAuthenticationProviders(plan.WebSite, plan.VirtualPath, removedProviders)
      );
    }

    // The workspace endpoint is the one place that does need an NTLM challenge.
    // We only roll this back if it was actually added here; we do not want to break
    // Windows authentication on the rolled-back version if it already has Windows auth enabled.
    var workspacePath = plan.VirtualPath.Trim('/', '\\') + "/api/auth/authenticate-workspace";
    if (_iis.AddAuthenticationProviders(plan.WebSite, workspacePath, ["NTLM"])) {
      rollback.Push("Removing the workspace NTLM provider...",
        () => _iis.RemoveWindowsAuthenticationProviders(plan.WebSite, workspacePath, ["NTLM"])
      );
    }

    WriteOptionAppSettings(plan);
  }

  private void WriteOptionAppSettings(InstallPlan plan) {
    var boundOptions = plan.Manifest.Options.Where(option => option.AppSetting is { Length: > 0 }).ToArray();
    if (boundOptions.Length == 0) {
      return;
    }

    var settings = AppSettingsFile.Open(plan.AppSettingsPath);
    foreach (var option in boundOptions) {
      settings.Set(option.AppSetting!, plan.Request.GetOption(option.Id) ?? option.DefaultValue);
    }
    settings.Save(plan.AppSettingsPath);
  }

  // [10] ────────────────────────────────────────────────────────────────────
  private void StartServices(InstallPlan plan, RollbackStack rollback, CancellationToken cancellationToken) {
    log.Step(10, TotalSteps, "Starting services...");

    ServiceManager.UnregisterSafe(LegacyServiceName, log);
    ServiceManager.UnregisterSafe(plan.ServiceName, log);

    ServiceManager.Register(
      plan.ServiceExecutablePath,
      plan.ServiceName,
      plan.ApplicationPoolName,
      Naming.ServiceDisplayName(plan.WebSite, plan.VirtualPath),
      log);

    rollback.Push("Stopping and removing the management service...", () => {
      ServiceManager.StopSafe(plan.ServiceName, log);
      ServiceManager.UnregisterSafe(plan.ServiceName, log);
    });

    log.Detail($"Starting management service ({plan.ServiceName})...");
    ServiceManager.Start(plan.ServiceName, log);

    EnsureApplicationPoolStarted(plan, cancellationToken);

    if (plan.WillEnableHttps) {
      log.Detail($"Enabling HTTPS on '{plan.WebSite}'...");
      _iis.AddHttpsBinding(plan.WebSite, InstallPlanner.HttpsPort);
    }

    if (plan.WillCreateCertificate) {
      log.Detail("Creating self-signed SSL certificate...");
      var thumbprint = CertificateManager.CreateSelfSigned(log);
      _iis.BindCertificate(plan.WebSite, InstallPlanner.HttpsPort, thumbprint, CertificateManager.StoreName);
    }
  }

  // [11] ────────────────────────────────────────────────────────────────────
  private void VerifyInstallation(InstallPlan plan, CancellationToken cancellationToken) {
    log.Step(11, TotalSteps, "Verifying installation...");

    if (plan.SkipHealthCheck) {
      log.Detail("Skipping health check.");
      log.Detail("If the application does not start correctly, you may need to restart the server or reinstall RAWeb.");
      return;
    }

    var (scheme, port) = ResolveActiveBinding(plan);
    var portSuffix = port == (plan.UsesHttps ? 443 : 80) ? "" : $":{port}";
    var url = $"{scheme}://localhost{portSuffix}/{plan.VirtualPath}/{plan.Manifest.Requirements.HealthCheckPath}";

    if (!HealthCheck.Poll(url, log, cancellationToken)) {
      throw new InstallFailedException("Health check failed. The application did not start correctly.");
    }
  }

  /// <summary>
  /// Since StartApplicationPool returns immediately, we need to poll until
  /// the application pool is actually started before we can continue to the next step.
  /// </summary>
  private void EnsureApplicationPoolStarted(InstallPlan plan, CancellationToken cancellationToken) {
    const int attempts = 30;

    _iis.StartApplicationPool(plan.ApplicationPoolName);

    for (var attempt = 1; attempt <= attempts; attempt++) {
      cancellationToken.ThrowIfCancellationRequested();

      if (_iis.IsApplicationPoolStarted(plan.ApplicationPoolName)) {
        return;
      }

      Thread.Sleep(1000);
    }

    log.Warning($"Application pool '{plan.ApplicationPoolName}' did not report as started after {attempts} seconds; proceeding anyway.");
  }

  // [12] ────────────────────────────────────────────────────────────────────
  private void CleanUp(InstallPlan plan) {
    log.Step(12, TotalSteps, "Cleaning up...");

    if (plan.IsUpgrade && plan.ExistingPhysicalPath is { Length: > 0 } && Directory.Exists(plan.ExistingPhysicalPath)) {
      log.Detail("Recycling app pool to release file locks...");
      _iis.RecycleApplicationPool(plan.ApplicationPoolName);

      var previousBaseDirectory = Path.GetDirectoryName(plan.ExistingPhysicalPath)!;
      log.Detail($"Removing previous version: {plan.ExistingPhysicalPath}");
      FileOperations.DeleteDirectoryIfExists(plan.ExistingPhysicalPath);

      var previousUninstallScript = Path.Combine(previousBaseDirectory, "uninstall.ps1");
      if (File.Exists(previousUninstallScript)) {
        File.Delete(previousUninstallScript);
      }

      FileOperations.RemoveEmptyAncestorDirectories(previousBaseDirectory);
    }

    if (plan.HasLegacyData) {
      log.Detail($"Removing legacy installation: {plan.LegacyPath}");
      ServiceManager.UnregisterSafe(LegacyServiceName, log);

      var legacyApplication = _iis.GetApplication(plan.Manifest.Defaults.WebSite, "RAWeb");
      if (legacyApplication is not null
        && legacyApplication.PhysicalPath.StartsWith(plan.LegacyPath, StringComparison.OrdinalIgnoreCase)) {
        _iis.RemoveApplication(plan.Manifest.Defaults.WebSite, "RAWeb");
      }

      FileOperations.DeleteDirectoryIfExists(plan.LegacyPath);
    }
  }

  // [13] ────────────────────────────────────────────────────────────────────
  private string RegisterInstallation(InstallPlan plan, RollbackStack rollback) {
    log.Step(13, TotalSteps, "Registering installation...");

    var request = new UninstallRegistrationRequest(
      plan.InstallDirectory,
      plan.VersionedDirectory,
      plan.WebSite,
      plan.VirtualPath,
      plan.ApplicationPoolName,
      plan.ServiceName,
      plan.Version,
      plan.Manifest.Product.Publisher,
      plan.InstalledMainExecutablePath,
      HttpPort(plan));

    var uninstallScriptPath = UninstallRegistration.WriteUninstallScript(request);

    log.Detail("Registering Add/Remove Programs entry...");
    UninstallRegistration.WriteRegistryEntry(request, uninstallScriptPath);

    rollback.Push("Removing Add/Remove Programs entry...", () => UninstallRegistration.RemoveRegistryEntry(plan.WebSite, plan.VirtualPath));

    return uninstallScriptPath;
  }

  private (string Scheme, int Port) ResolveActiveBinding(InstallPlan plan) {
    var bindings = _iis.GetBindings(plan.WebSite);
    var protocol = plan.UsesHttps ? "https" : "http";

    var binding = bindings.FirstOrDefault(candidate => candidate.Protocol == protocol)
      ?? bindings.FirstOrDefault(candidate => candidate.Protocol == "http");

    return (protocol, binding?.Port ?? (plan.UsesHttps ? 443 : 80));
  }

  private int HttpPort(InstallPlan plan) =>
    _iis.GetBindings(plan.WebSite).FirstOrDefault(binding => binding.Protocol == "http")?.Port ?? 80;

  private (string WebInterface, string Workspace) BuildUrls(InstallPlan plan) {
    var (scheme, port) = ResolveActiveBinding(plan);
    var portSuffix = port == (plan.UsesHttps ? 443 : 80) ? "" : $":{port}";
    var baseUrl = $"{scheme}://{Environment.MachineName}{portSuffix}/{plan.VirtualPath}";
    return (baseUrl, baseUrl + "/webfeed.aspx");
  }

  private static string Format(TimeSpan elapsed) =>
    elapsed.TotalMinutes >= 1 ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s" : $"{elapsed.Seconds}s";
}
