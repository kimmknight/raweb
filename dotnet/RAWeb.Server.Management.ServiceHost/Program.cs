using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;

/// <summary>
/// Installs the RAWeb Management Service.
/// <br /><br />
/// To install the service, run the following command from an elevated command prompt:
/// <br /><br />
/// ```
/// sc create RAWebManagementService binPath= "C:\Path\To\RAWeb.Server.Management.ServiceHost.exe" start= auto
/// ```
/// </summary>
[RunInstaller(true)]
public class ProjectInstaller : Installer {
  public ProjectInstaller() {
    var processInstaller = new ServiceProcessInstaller {
      Account = ServiceAccount.LocalSystem
    };

#if RELEASE
    const string serviceName = "RAWebManagementService";
    const string serviceDisplayName = "RAWeb Management Service";
#else
    const string serviceName = "RAWebManagementService-Dev";
    const string serviceDisplayName = "RAWeb Management Service (Development)";
#endif

    var serviceInstaller = new ServiceInstaller {
      ServiceName = serviceName,
      DisplayName = serviceDisplayName,
      Description = "Performs privileged operations for RAWeb.",
      StartType = ServiceStartMode.Automatic
    };

    Installers.Add(processInstaller);
    Installers.Add(serviceInstaller);
  }
}

/// <summary>
/// The main entry point for the management service.
/// </summary>
static class Program {
  static void Main(string[] args) {
    // install or uninstall the service if running in interactive mode
    // RAWeb.Server.Management.ServiceHost.exe install - installs the service
    // RAWeb.Server.Management.ServiceHost.exe uninstall - uninstalls the service
    if (Environment.UserInteractive) {
      if (args.Length > 0) {
        // install or uninstall requires admin rights,
        // so relaunch the process as admin if necessary
        if (!IsAdministrator()) {
          RelaunchAsAdmin();
          return;
        }

        switch (args[0].ToLowerInvariant()) {
          case "install":
            ManagedInstallerClass.InstallHelper([Assembly.GetExecutingAssembly().Location]);
            return;
          case "uninstall":
            ManagedInstallerClass.InstallHelper(["/u", Assembly.GetExecutingAssembly().Location]);
            return;
        }
      }
      return;
    }

    // run the service when not launching the executable interactively
    ServiceBase.Run(
      [
        new ManagementService()
      ]
    );
  }

  private static bool IsAdministrator() {
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }

  private static void RelaunchAsAdmin() {
    var exePath = Assembly.GetExecutingAssembly().Location;
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
