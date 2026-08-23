using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RAWeb.Server.Installer.Setup;

public sealed class StartupOptions {
  public const string ResumeSwitch = "--resume";

  /// <summary>
  /// Path to the handoff file written by the unelevated instance, when this is the elevated one.
  /// </summary>
  public string? ResumeFilePath { get; private set; }

  public bool IsResumedAfterElevation => ResumeFilePath is { Length: > 0 };

  public static StartupOptions Parse(IReadOnlyList<string> args) {
    var options = new StartupOptions();

    for (var i = 0; i < args.Count - 1; i++) {
      if (string.Equals(args[i], ResumeSwitch, StringComparison.OrdinalIgnoreCase)) {
        options.ResumeFilePath = args[i + 1];
      }
    }

    return options;
  }
}

public static class ElevationHelper {
  public static bool IsElevated => SystemChecks.IsRunningAsAdministrator();

  /// <summary>
  /// Starts a second copy of the installer elevated, handing over everything the user has chosen so
  /// far.
  /// </summary>
  /// <returns>
  /// True when the elevated process started, meaning this process should exit. False when the user
  /// dismissed the UAC prompt, so the wizard can stay where it is and let them retry.
  /// </returns>
  public static bool RelaunchElevated(HandoffState handoff) {
    var executablePath = Assembly.GetEntryAssembly()?.Location ?? Process.GetCurrentProcess().MainModule?.FileName;

    if (executablePath is not { Length: > 0 }) {
      throw new InstallFailedException("Could not determine the installer's own path in order to elevate.");
    }

    var handoffPath = handoff.Save();

    var startInfo = new ProcessStartInfo(executablePath, $"{StartupOptions.ResumeSwitch} \"{handoffPath}\"") {
      UseShellExecute = true,
      Verb = "runas",
    };

    try {
      if (Process.Start(startInfo) is not null) {
        return true;
      }
    }
    catch (Win32Exception exception) when (exception.NativeErrorCode == 1223) {
      // ERROR_CANCELLED: the user dismissed the UAC prompt.
    }

    TryDelete(handoffPath);
    return false;
  }

  private static void TryDelete(string path) {
    try {
      File.Delete(path);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
    }
  }
}
