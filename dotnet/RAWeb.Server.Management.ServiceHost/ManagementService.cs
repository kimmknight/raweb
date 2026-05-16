using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using RAWeb.Server.Management;
using static RAWeb.Server.Management.SystemRemoteApps;

/// <summary>
/// The RAWeb Management Service. It hosts a named-pipe server that
/// allows unprivileged RAWeb processes to perform registry and
/// resource managementoperations that require elevated privileges.
/// </summary>
public class ManagementService : ServiceBase {
  private NamedPipeServer? _pipeServer;

  protected override void OnStart(string[] args) {
    // stop any existing conflicting pipe server
    // to ensure there are no address conflicts
    _pipeServer?.Stop();

    // read the app pool name passed by the installer via --app-pool <name>
    var appPoolName = Environment.GetCommandLineArgs()
        .SkipWhile(a => a != "--app-pool")
        .Skip(1)
        .FirstOrDefault() ?? "raweb";

    // create and start the named pipe for the management service
    _pipeServer = new NamedPipeServer(appPoolName);
    _pipeServer.Start();
  }

  protected override void OnStop() {
    _pipeServer?.Stop();
  }
}

/// <summary>
/// Implements <see cref="IManagementServiceHost"/> by delegating directly to the
/// RAWeb.Server.Management classes. Authorization is enforced by the pipe access
/// riles in <see cref="NamedPipeServer"/>.
/// </summary>
public class SystemRemoteAppsServiceHost : IManagementServiceHost {
  public void WriteRemoteAppToRegistry(SystemRemoteApp app) {
    if (app is null) {
      throw new ArgumentNullException(nameof(app));
    }

    app.WriteToRegistry();
  }

  public void DeleteRemoteAppFromRegistry(SystemRemoteApp app) {
    if (app is null) {
      throw new ArgumentNullException(nameof(app));
    }

    app.DeleteFromRegistry();
  }

  public void RestorePackagedAppIconPaths(string? collectionName) {
    new SystemRemoteApps(collectionName).GetAllRegisteredApps(restorePackagedAppIconPaths: true);
  }

  public InstalledApps ListInstalledApps(string? userSid) {
    var packagedApps = InstalledApps.FromAppPackages().Concat(InstalledApps.FromStartMenu());
    var startMenuApps = InstalledApps.FromStartMenu();
    var userStartMenuApps = userSid is null ? [] : InstalledApps.FromStartMenu(new SecurityIdentifier(userSid));
    return new InstalledApps([.. packagedApps, .. startMenuApps, .. userStartMenuApps]);
  }

  public void InitializeRegistryPaths(string? collectionName = null) {
    new SystemRemoteApps(collectionName).EnsureRegistryPathExists();
  }

  public void InitializeDesktopRegistryPaths(string collectionName) {
    var defaultDesktop = SystemDesktop.FromRegistry(collectionName, collectionName);
    if (defaultDesktop is null) {
      defaultDesktop = new SystemDesktop(collectionName, collectionName);
      defaultDesktop.WriteToRegistry();
    }
  }

  public void WriteDesktopToRegistry(SystemDesktop desktop) {
    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    desktop.WriteToRegistry();
  }

  public void DeleteDesktopFromRegistry(SystemDesktop desktop) {
    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    desktop.DeleteFromRegistry();
  }

  public Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid) {
    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    var stream = desktop.GetWallpaperStream(theme, userSid is null ? null : new SecurityIdentifier(userSid));
    return stream;
  }

  public bool AreConnectionsAllowed() => SystemTerminalServerSettings.AreConnectionsAllowed;
}
