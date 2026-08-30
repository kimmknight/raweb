using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Registers, starts, and removes the RAWeb management service.
/// </summary>
public static class ServiceManager {
  public static bool Exists(string serviceName) => ServiceController.GetServices().Any(service =>
    string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase)
  );

  /// <summary>
  /// Stops the service and then kills the process by PID if it did not exit. Killing by PID rather than
  /// by service name matters because several RAWeb installations can run simultaneously
  /// while using the same service name (but different exe path).
  /// </summary>
  public static void StopSafe(string serviceName, InstallLog log) {
    if (!Exists(serviceName)) {
      return;
    }

    var processId = GetServiceProcessId(serviceName);

    try {
      using var controller = new ServiceController(serviceName);
      if (controller.Status is not (ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)) {
        controller.Stop();
      }
      controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
    }
    catch (Exception exception) when (exception is InvalidOperationException or System.ServiceProcess.TimeoutException) {
    }

    if (processId > 0) {
      try {
        using var process = Process.GetProcessById(processId);
        log.Detail($"Service '{serviceName}' did not stop gracefully; killing service process {processId}...");
        process.Kill();
        process.WaitForExit(10000);
      }
      catch (ArgumentException) {
        // already gone
      }
    }

    Thread.Sleep(1000);
  }

  public static void UnregisterSafe(string serviceName, InstallLog log) {
    if (!Exists(serviceName)) {
      return;
    }

    StopSafe(serviceName, log);
    ProcessRunner.Run(ScPath, $"delete \"{serviceName}\"");
    Thread.Sleep(500);
  }

  public static void Register(string executablePath, string serviceName, string applicationPoolName, string displayName, InstallLog log) {
    if (!File.Exists(executablePath)) {
      log.Warning($"Service executable not found: {executablePath}");
      return;
    }

    var binaryPath = $"\\\"{executablePath}\\\" --app-pool \\\"{applicationPoolName}\\\"";

    var create = ProcessRunner.Run(
      ScPath,
      $"create \"{serviceName}\" binPath= \"{binaryPath}\" start= auto DisplayName= \"{displayName}\""
    );

    if (!create.Succeeded) {
      throw new InstallFailedException(
        $"Could not register service '{serviceName}' (sc.exe exit code {create.ExitCode}): {create.StandardOutput.Trim()}"
      );
    }

    ProcessRunner.Run(ScPath, $"description \"{serviceName}\" \"This service performs privileged operations for RAWeb.\"");
    ProcessRunner.Run(ScPath, $"failure \"{serviceName}\" reset= 86400 actions= restart/5000/restart/10000/restart/30000");
  }

  public static void Start(string serviceName, InstallLog log) {
    if (!Exists(serviceName)) {
      log.Warning($"Service '{serviceName}' was not found after registration.");
      return;
    }

    using var controller = new ServiceController(serviceName);
    if (controller.Status == ServiceControllerStatus.Running) {
      return;
    }

    controller.Start();
    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
  }

  private static int GetServiceProcessId(string serviceName) {
    var escaped = serviceName.Replace("'", "''");
    using var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Service WHERE Name='{escaped}'");
    foreach (var entry in searcher.Get()) {
      using var service = (ManagementObject)entry;
      return Convert.ToInt32(service["ProcessId"]);
    }
    return 0;
  }

  private static string ScPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe"
  );
}
