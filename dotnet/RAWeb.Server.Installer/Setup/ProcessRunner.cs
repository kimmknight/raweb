using System.Diagnostics;
using System.IO;
using System.Text;

namespace RAWeb.Server.Installer.Setup;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError) {
  public bool Succeeded => ExitCode == 0;

  public IEnumerable<string> OutputLines =>
    StandardOutput.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Runs the external tools the engine cannot replace with a managed API (dism, sc, iisreset, powershell).
/// </summary>
public static class ProcessRunner {
  public static ProcessResult Run(
    string fileName,
    string arguments,
    InstallLog? log = null,
    bool echoOutput = false,
    string? workingDirectory = null,
    string? prependToPath = null) {
    var startInfo = new ProcessStartInfo(fileName, arguments) {
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      StandardOutputEncoding = Encoding.UTF8,
      StandardErrorEncoding = Encoding.UTF8,
      WorkingDirectory = workingDirectory ?? "",
    };

    if (prependToPath is { Length: > 0 }) {
      startInfo.EnvironmentVariables["PATH"] = prependToPath + ";" + startInfo.EnvironmentVariables["PATH"];
    }

    startInfo.EnvironmentVariables["FORCE_COLOR"] = "1";

    var standardOutput = new StringBuilder();
    var standardError = new StringBuilder();

    using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

    process.OutputDataReceived += (_, e) => {
      if (e.Data is null) {
        return;
      }
      standardOutput.AppendLine(e.Data);
      if (echoOutput && e.Data.Trim().Length > 0) {
        log?.Detail(e.Data.TrimEnd());
      }
    };
    process.ErrorDataReceived += (_, e) => {
      if (e.Data is null) {
        return;
      }
      standardError.AppendLine(e.Data);
      if (echoOutput && e.Data.Trim().Length > 0) {
        log?.Warning("  " + e.Data.TrimEnd());
      }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    return new ProcessResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
  }

  /// <summary>
  /// Runs a Windows PowerShell 5 command. Reserved for Windows Server feature querying and
  /// installation, which is only exposed through the ServerManager module.
  /// </summary>
  public static ProcessResult RunPowerShell(string script, InstallLog? log = null, bool echoOutput = false) {
    var powerShell = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.System),
      "WindowsPowerShell",
      "v1.0",
      "powershell.exe"
    );

    var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    return Run(powerShell, $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}", log, echoOutput);
  }

  /// <summary>
  /// Launches a process visibly and waits, used for the Hosting Bundle installer.
  /// </summary>
  public static int RunAndWaitVisible(string fileName, string arguments) {
    using var process = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true })
      ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
    process.WaitForExit();
    return process.ExitCode;
  }
}
