using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;

namespace RAWeb.Server.Management.ServiceHost;

/// <summary>
/// The main entry point for the management service.
/// </summary>
static class Program {
  static void Main(string[] args) {
    // install or uninstall the service if running in interactive mode
    if (Environment.UserInteractive) {
      if (!IsAdministrator()) {
        Console.WriteLine("Administrator rights are required to install or uninstall the service. Relaunching as administrator...");
        RelaunchAsAdmin();
        return;
      }

      Console.WriteLine("This executable is intended to be run as a Windows Service and does not have a user interface.");

      var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path.");

      var isAlreadyInstalled = FindServiceByPath(exePath) != null;
      if (isAlreadyInstalled) {
        Console.WriteLine("The service is already installed.");
        Console.WriteLine("Do you want to uninstall the existing service? (y/N) ");
        while (true) {
          var response = Console.ReadLine();
          if (string.IsNullOrEmpty(response) || response.Equals("n", StringComparison.OrdinalIgnoreCase)) {
            Console.WriteLine("Service uninstallation cancelled.");
            return;
          }
          else if (response.Equals("y", StringComparison.OrdinalIgnoreCase)) {
            Console.WriteLine("Stopping service...");
            RunSc($"stop \"{FindServiceByPath(exePath)}\"");
            Console.WriteLine("Uninstalling service...");
            RunSc($"delete \"{FindServiceByPath(exePath)}\"");
            Console.WriteLine("Existing service uninstalled.");
            break;
          }
          else {
            Console.WriteLine("Please enter 'y' to uninstall the existing service or 'n' to cancel.");
          }
        }
      }
      else {
        Console.WriteLine("Do you want to install this service? (y/N) ");
        while (true) {
          var response = Console.ReadLine();
          if (string.IsNullOrEmpty(response) || response.Equals("n", StringComparison.OrdinalIgnoreCase)) {
            Console.WriteLine("Service installation cancelled.");
            return;
          }
          else if (response.Equals("y", StringComparison.OrdinalIgnoreCase)) {
            break;
          }
          else {
            Console.WriteLine("Please enter 'y' to install the service or 'n' to cancel.");
          }
        }

        Console.WriteLine("What is the name of the IIS application pool under which RAWeb is running? (default: raweb) ");
        var appPoolName = Console.ReadLine();
        if (string.IsNullOrEmpty(appPoolName)) {
          appPoolName = "raweb";
        }

        Console.WriteLine("Enter any additional SIDs that should have read/write access to the pipe (comma-separated), enter me for your current user, or leave blank for none:");
        var additionalSidsInput = Console.ReadLine();
        var additionalSidsArg = "";
        if (!string.IsNullOrEmpty(additionalSidsInput)) {
          var sids = additionalSidsInput
              .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .ToArray();
          if (sids.Contains("me", StringComparer.OrdinalIgnoreCase)) {
            var currentUserSid = WindowsIdentity.GetCurrent().User?.Value;
            if (!string.IsNullOrEmpty(currentUserSid)) {
              sids = sids.Select(s => s.Equals("me", StringComparison.OrdinalIgnoreCase) ? currentUserSid : s).ToArray();
            }
          }
          additionalSidsArg = $"--additional-access-sids {string.Join(",", sids)}";
        }

        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);

#if RELEASE
      var serviceName = $"RAWebManagementService-{guid}";
      var serviceDisplayName = $"RAWeb Management Service ({appPoolName})";
#else
        var serviceName = $"RAWebManagementService-Dev-{guid}";
        var serviceDisplayName = $"RAWeb Management Service ({appPoolName}) (Development)";
#endif

        Console.WriteLine($"What do you want to call this service? (default: {serviceDisplayName}) ");
        var inputServiceDisplayName = Console.ReadLine();
        if (!string.IsNullOrEmpty(inputServiceDisplayName)) {
          serviceDisplayName = inputServiceDisplayName;
        }

        Console.WriteLine("Installing service...");
        var installArgs = $"create \"{serviceName}\" binPath= \"{exePath} --app-pool {appPoolName} {additionalSidsArg}\" start= auto DisplayName= \"{serviceDisplayName}\"";
        RunSc(installArgs);
        RunSc($"description \"{serviceName}\" \"This service performs privileged operations for RAWeb.\"");

        Console.WriteLine("Starting service...");
        RunSc($"start \"{serviceName}\"");

        Console.WriteLine("Service installed successfully.");
      }

      // wait before closing
      Console.WriteLine("Press Enter to exit...");
      Console.ReadLine();

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
      Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1)),
      UseShellExecute = true,
    };
    try {
      Process.Start(psi);
    }
    catch {
      Console.WriteLine("Administrator rights are required.");
    }
  }

  /// <summary>
  /// Searches the registry for an existing service that has the same executable path
  /// as the exePath argument.
  /// </summary>
  /// <param name="exePath"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  static string? FindServiceByPath(string exePath) {
    using var servicesKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
      @"SYSTEM\CurrentControlSet\Services"
    );

    if (servicesKey == null) {
      throw new InvalidOperationException("Cannot open services registry key.");
    }

    foreach (var serviceName in servicesKey.GetSubKeyNames()) {
      using var serviceKey = servicesKey.OpenSubKey(serviceName);
      var imagePath = (serviceKey?.GetValue("ImagePath") as string)?.Trim('"');

      // remove command-line arguments from the path before we compare it to the current executable's path
      if (imagePath != null) {
        var firstSpaceIndex = imagePath.IndexOf(' ');
        if (firstSpaceIndex != -1) {
          imagePath = imagePath.Substring(0, firstSpaceIndex);
        }
      }

      if (string.Equals(imagePath, exePath, StringComparison.OrdinalIgnoreCase)) {
        return serviceName;
      }
    }

    return null;
  }
}
