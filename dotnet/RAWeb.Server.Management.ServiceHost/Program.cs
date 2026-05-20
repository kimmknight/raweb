using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;

/// <summary>
/// The main entry point for the management service.
/// <br /><br />
/// To install the service, run:
///   RAWeb.Server.Management.ServiceHost.exe install
/// To uninstall:
///   RAWeb.Server.Management.ServiceHost.exe uninstall
/// </summary>
static class Program {
  static void Main(string[] args) {
    // install or uninstall the service if running in interactive mode
    if (Environment.UserInteractive) {
      if (args.Length > 0) {
        // install or uninstall requires admin rights,
        // so relaunch the process as admin if necessary
        if (!IsAdministrator()) {
          RelaunchAsAdmin();
          return;
        }

#if RELEASE
        const string serviceName = "RAWebManagementService";
        const string serviceDisplayName = "RAWeb Management Service";
#else
        const string serviceName = "RAWebManagementService-Dev";
        const string serviceDisplayName = "RAWeb Management Service (Development)";
#endif
        var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path.");

        switch (args[0].ToLowerInvariant()) {
          case "install":
            RunSc($"create {serviceName} binPath= \"{exePath}\" start= auto DisplayName= \"{serviceDisplayName}\"");
            RunSc($"description {serviceName} \"Performs privileged operations for RAWeb.\"");
            return;
          case "uninstall":
            RunSc($"delete {serviceName}");
            return;
        }
      }
      return;
    }

    // run the service when not launching the executable interactively
    ServiceBase.Run([new ManagementService()]);
  }

  private static void RunSc(string arguments) {
    using var proc = Process.Start(new ProcessStartInfo {
      FileName = "sc.exe",
      Arguments = arguments,
      UseShellExecute = false
    });
    proc?.WaitForExit();
  }

  private static bool IsAdministrator() {
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }

  private static void RelaunchAsAdmin() {
    var exePath = Environment.ProcessPath ?? "";
    var psi = new ProcessStartInfo {
      FileName = exePath,
      Verb = "runas", // triggers UAC
      Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1))
    };
    try {
      Process.Start(psi);
    }
    catch {
      Console.WriteLine("Administrator rights are required.");
    }
  }
}
