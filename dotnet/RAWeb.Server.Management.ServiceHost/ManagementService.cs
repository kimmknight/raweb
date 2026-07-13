using System;
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
  }

  protected override void OnStop() {
    _pipeServer?.Stop();
  }
}
