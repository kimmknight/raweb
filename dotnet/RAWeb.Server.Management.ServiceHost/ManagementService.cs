using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;

namespace RAWeb.Server.Management.ServiceHost;

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

    // read additional SIDs that should have read/write access to the pipe from the
    // the --additional-access-sids argument passed by the installer (comma-delimited list of SIDs)
    var additionalAccessSids = Environment.GetCommandLineArgs()
        .SkipWhile(a => a != "--additional-access-sids")
        .Skip(1)
        .FirstOrDefault()?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => new SecurityIdentifier(s))
        .ToArray();

    // create and start the named pipe for the management service
    _pipeServer = new NamedPipeServer(appPoolName, additionalAccessSids);
    _pipeServer.Start();

    EnsureAppSettingsFileExists();
  }

  /// <summary>
  /// Creates an empty App_Data\appSettings.config if it is missing.
  /// <br /><br />
  /// RAWeb's web.config loads its application settings from this file, so IIS fails every
  /// request with a configuration error (500.19) when the file is missing. RAWeb cannot create
  /// the file itself because IIS fails before it starts RAWeb. This service is installed in the
  /// same folder as RAWeb and runs independently of IIS.
  /// </summary>
  private static void EnsureAppSettingsFileExists() {
    var appDataPath = Path.Combine(AppContext.BaseDirectory, "App_Data");
    var appSettingsPath = Path.Combine(appDataPath, "appSettings.config");

    if (File.Exists(appSettingsPath)) {
      return;
    }

    try {
      Directory.CreateDirectory(appDataPath);

      // CreateNew fails if another process created the file after
      // the check above, so it is never overwritten
      using var stream = new FileStream(appSettingsPath, FileMode.CreateNew, FileAccess.Write);
      using var writer = new StreamWriter(stream);
      writer.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<appSettings />\r\n");
    }
    catch (Exception) {
    }
  }

  protected override void OnStop() {
    _pipeServer?.Stop();
  }
}
