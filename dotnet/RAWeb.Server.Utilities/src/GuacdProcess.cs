using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWeb.Server.Utilities;

public static class Guacd {
    private static string imagePath => Path.Combine(Constants.AppRoot, "bin", $"guacd.wsl");
    private static string imageDate => File.GetLastWriteTimeUtc(imagePath).ToString("o");
    private static string containerNamePrefix => $"guacd-{AppId.ToGuid()}";
    private static string containerName => $"{containerNamePrefix}-{imageDate.GetHashCode():X8}";
    private static readonly object s_lock = new();
    private static Task? s_worker;
    private static CancellationTokenSource? s_cts;
    private static readonly ManualResetEventSlim s_started = new();

    /// <summary>
    /// Whether guacd is currently running.
    /// </summary>
    public static bool IsRunning {
        get {
            lock (s_lock) {
                return s_worker != null && !s_worker.IsCompleted && IsGuacdHealthy;
            }
        }
    }

    /// <summary>
    /// Extract the IP address from the guacd WSL distribution. This is needed because WSL2 uses a
    /// virtualizednetwork interface with a dynamic IP address, so we can’t rely on localhost to
    /// connect to guacd from the web server.
    /// </summary>
    public static string IpAddress {
        get {
            var output = RunWithOutput(
                @"C:\Program Files\WSL\wsl.exe",
                $"-d {containerName} -- sh -c \"ip addr show eth0 | sed -n 's/.*inet \\([0-9.]*\\)\\/.*/\\1/p'\"",
                out var exitCode
            ).Trim();
            if (exitCode != 0 || string.IsNullOrWhiteSpace(output)) {
                return "";
            }
            return output;
        }
    }

    static readonly Logger s_logger = new("guacd");

    private static void WriteLogline(string line, bool writeToConsole = false) {
        // remove preceding occurences of "guacd[pid]: "
        var cleanedLine = System.Text.RegularExpressions.Regex.Replace(line, @"^guacd\[\d+\]:\s*", "");

        s_logger.WriteLogline(cleanedLine, writeToConsole);
    }

    /// <summary>
    /// The last exception that occurred in the background task, if any.
    /// </summary>
    public static Exception? LastException {
        get {
            return s_worker?.IsFaulted == true ? s_worker.Exception : null;
        }
        private set { }
    }

    /// <summary>
    /// Whether this operating system supports WSL2 and the
    /// guacd.wsl distribution is available.
    /// 
    /// For x64 systems, build 18362.1049 or later is required.
    /// For ARM64 systems, build 19041 or later is required.
    /// 
    /// guacd.wsl must be in the bin folder.
    /// </summary>
    public static bool IsWindowsSubsystemForLinuxSupported {
        get {
            var isGuacdDistroAvailable = File.Exists(imagePath);
            if (!isGuacdDistroAvailable) {
                return false;
            }

            var osVersion = Environment.OSVersion.Version;
            if (Environment.Is64BitOperatingSystem) {
                if (osVersion.Major > 10) {
                    return true;
                }
                if (osVersion.Major == 10 && osVersion.Build > 18362) {
                    return true;
                }
                if (osVersion.Major == 10 && osVersion.Build == 18362 && osVersion.Revision >= 1049) {
                    return true;
                }
                return false;
            }
            else {
                if (osVersion.Major > 10) {
                    return true;
                }
                if (osVersion.Major == 10 && osVersion.Build >= 19041) {
                    return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Checks whether WSL is installed to C:\Program Files\WSL\wsl.exe.
    /// </summary>
    public static bool IsWindowsSubsystemForLinuxInstalled => File.Exists(@"C:\Program Files\WSL\wsl.exe");

    /// <summary>
    /// Checks the status of WSL and throws exceptions if WSL is not properly installed or configured.
    /// 
    /// In particular, if the Windows Subsystem for Linux optional component or the Virtual Machine Platform
    /// optional component are missing, this method will throw specific exceptions for those cases.
    /// All other errors related to WSL will throw a generic UnknownWslErrorCodeException.
    /// </summary>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
    /// <exception cref="MissingOptionalComponentException"></exception>
    /// <exception cref="VirtualMachinePlatformMissingException"></exception>
    /// <exception cref="UnknownWslErrorCodeException"></exception>
    private static void ConfirmWslIsReady() {
        if (!IsWindowsSubsystemForLinuxInstalled) {
            throw new WindowsSubsystemForLinuxMissingException();
        }

        // check for WSL errors
        var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", "--status", out var exitCode, Encoding.Unicode);
        if (exitCode == 0) {
            return; // WSL has all required components
        }
        var errorCode = ExtractWslErrorCode(output);

        if (errorCode == "WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED") {
            WriteLogline($"[Manager] ERROR: WSL optional component is missing. (Wsl/{errorCode})");
            throw new MissingOptionalComponentException();
        }
        if (errorCode == "WSL_E_VIRTUAL_MACHINE_PLATFORM_REQUIRED") {
            WriteLogline($"[Manager] ERROR: WSL2 Virtual Machine Platform optional component is missing. (Wsl/{errorCode})");
            throw new VirtualMachinePlatformMissingException();
        }
        WriteLogline($"[Manager] ERROR: Unknown WSL error occurred. (Wsl/{errorCode})");
        throw new UnknownWslErrorCodeException(errorCode);
    }

    /// <summary>
    /// Extracts the WSL error code from the output of a WSL command.
    /// In most cases, the error code is in a line containing "Wsl/*" and "_E_"
    /// at the end of the output of a WSL command.
    /// </summary>
    /// <param name="output"></param>
    /// <returns></returns>
    private static string ExtractWslErrorCode(string output) {
        var lines = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        var errorLine = lines.LastOrDefault(line => line.Contains("Wsl/") && line.Contains("_E_"));
        var errorCodeStartIndex = errorLine?.IndexOf("Wsl/") + 4;
        var errorCodeEndIndex = errorLine?.IndexOf(' ', errorCodeStartIndex ?? 0) ?? -1;
        string? errorCode = null;
        if (errorLine != null && errorCodeStartIndex.HasValue && errorCodeStartIndex.Value >= 0) {
            if (errorCodeEndIndex > errorCodeStartIndex) {
                errorCode = errorLine.Substring(errorCodeStartIndex.Value, errorCodeEndIndex - errorCodeStartIndex.Value);
            }
            else {
                errorCode = errorLine.Substring(errorCodeStartIndex.Value);
            }
        }
        return errorCode ?? "Unknown";
    }


    /// <summary>
    /// Checks whether the guacd WSL distribution is installed.
    /// This checks the names of the installed WSL distributions for a distribution named "guacd-{appId}",
    /// where {appId} is the id of the current RAWeb installation.
    /// </summary>
    public static bool IsGuacdDistributionInstalled => IsWindowsSubsystemForLinuxInstalled && AllInstalledDistributions.Contains(containerName);

    /// <summary>
    /// Gets the names of all installed WSL distributions by running "wsl --list --quiet" and
    /// parsing the output.
    /// </summary>
    private static string[] AllInstalledDistributions {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return [];
            }
            var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", "--list --quiet", out var exitCode, Encoding.Unicode);
            if (exitCode != 0) {
                return [];
            }
            var distros = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            return distros;
        }
    }

    /// <summary>
    /// Gets the names of all installed guacd WSL distributions for this RAWeb installation
    /// by checking <see cref="AllInstalledDistributions"/>.
    /// <br />
    /// Each guacd distribution is named "guacd-{appId}-{imageDateHash}",
    /// where {appId} is the id of the current RAWeb installation and
    /// {imageDateHash} is a hash of the last write time of the guacd.wsl image file.
    /// </summary>
    private static string[] AllGuacdDistributions => [.. AllInstalledDistributions.Where(distro => distro.StartsWith(containerNamePrefix))];

    /// <summary>
    /// Gets the names of all installed guacd WSL distributions for this RAWeb installation
    /// by checking <see cref="AllGuacdDistributions"/>, excluding the current distribution.
    /// </summary>
    private static string[] AllOldGuacdDistributions => [.. AllGuacdDistributions.Where(distro => distro != containerName)];

    /// <summary>
    /// Checks whether the guacd WSL distribution is currently running.
    /// This checks the names of the running WSL distributions for a distribution
    /// named "guacd-{appId}",
    /// </summary>
    private static bool IsGuacdDistributionRunning => IsWindowsSubsystemForLinuxInstalled && AllRunningDistriubtions.Any(distro => distro.Trim() == containerName);

    /// <summary>
    /// Gets the names of all running WSL distributions by running "wsl --list --quiet --running"
    /// and parsing the output.
    /// </summary>
    private static string[] AllRunningDistriubtions {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return [];
            }
            var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", "--list --quiet --running", out var exitCode, Encoding.Unicode);
            if (exitCode != 0) {
                return [];
            }
            var distros = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            return distros;
        }
    }

    /// <summary>
    /// Checks whether guacd is healthy. Checks whether the quacd port is accepting connections.
    /// If guacd crashed (or never fully started), the port will not respond to connections.
    /// </summary>
    /// <returns></returns>
    private static bool IsGuacdHealthy {
        get {
            if (!IsGuacdDistributionRunning) {
                return false;
            }

            try {
                using var client = new System.Net.Sockets.TcpClient();
                var task = client.ConnectAsync(IpAddress, 4822);
                var result = task.Wait(2000); // wait for 2 seconds
                return result && client.Connected;
            }
            catch {
                return false;
            }
        }
    }

    /// <summary>
    /// Installs the guacd WSL distribution if it is not already installed. This imports
    /// a WSL distribution from a tarball included with the application.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
    /// <exception cref="MissingOptionalComponentException"></exception>
    /// <exception cref="VirtualMachinePlatformMissingException"></exception>
    /// <exception cref="VirtualMachinePlatformUnavailableException"></exception>
    /// <exception cref="UnknownWslErrorCodeException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="GuacdInstallFailedException"></exception>
    public static string? InstallGuacd() {
        ConfirmWslIsReady();

        if (IsGuacdDistributionInstalled) {
            return null;
        }

        if (!File.Exists(imagePath)) {
            throw new FileNotFoundException($"Guacd wsl image not found at {imagePath}");
        }

        // import the guacd distribution from the tarball using wsl --import
        var installLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RAWeb", "containers", containerName);
        Directory.CreateDirectory(installLocation);

        var output = UninstallGuacd();
        output += UninstallOldGuacdDistributions();

        var file = @"C:\Program Files\WSL\wsl.exe";
        var args = $"--import {containerName} {installLocation} {imagePath} --version 2";
        output += file + " " + args + Environment.NewLine;
        output += RunWithOutput(file, args, out var exitCode);
        if (exitCode != 0) {
            WriteLogline("[Manager] ERROR: Failed to install guacd WSL distribution: " + output, true);

            // WSL2 requires the Virtual Machine Platform optional component, but wsl --status will not
            // always report that it is missing, so we must check here as well.
            var errorCode = ExtractWslErrorCode(output);
            if (errorCode.Contains("HCS_E_HYPERV_NOT_INSTALLED")) {
                throw new VirtualMachinePlatformMissingException();
            }
            if (errorCode.Contains("HCS_E_SERVICE_NOT_AVAILABLE")) {
                // this can happen if the the CPU does not support virtualization,
                // it is disabled in the BIOS, or nested virtualization is unsupported
                // or not exposed to the VM (if running inside a VM)
                throw new VirtualMachinePlatformUnavailableException();
            }
            if (errorCode is not null) {
                throw new UnknownWslErrorCodeException(errorCode);
            }

            throw new GuacdInstallFailedException("Failed to install guacd WSL distribution: " + output);
        }
        return output;
    }

    /// <summary>
    /// Uninstalls the guacd WSL distribution.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
    public static string? UninstallGuacd() {
        if (!IsWindowsSubsystemForLinuxInstalled) {
            throw new WindowsSubsystemForLinuxMissingException();
        }

        if (!IsGuacdDistributionInstalled) {
            return null;
        }

        var file = @"C:\Program Files\WSL\wsl.exe";
        var args = $"--unregister {containerName}";
        var output = RunWithOutput(file, args, out _);
        return file + " " + args + Environment.NewLine + output;
    }

    /// <summary>
    /// Uninstalls any old guacd WSL distributions that do not match the current image date.
    /// RAWeb checks the last write time of the guacd.wsl image file and includes it in the
    /// WSL distribution name. This allows RAWeb to detect when the guacd image has been updated
    /// and uninstall any old distributions that are no longer needed.
    /// </summary>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
    public static string UninstallOldGuacdDistributions() {
        if (!IsWindowsSubsystemForLinuxInstalled) {
            throw new WindowsSubsystemForLinuxMissingException();
        }

        var output = new StringBuilder();

        foreach (var distro in AllOldGuacdDistributions) {
            WriteLogline($"[Manager] INFO: Uninstalling old guacd distribution {distro}...", true);
            var file = @"C:\Program Files\WSL\wsl.exe";
            var args = $"--unregister {distro}";
            output.AppendLine(file + " " + args);
            output.Append(RunWithOutput(file, args, out _));
        }

        return output.ToString();
    }

    /// <summary>
    /// Attempts to start guacd in a WSL distribution. If guacd is already running, this method does nothing.
    /// If the WSL distribution is not present or fails to start, this method throws an exception. Note that
    /// guacd is required for the web client to function, so if this method fails, the web client will not be
    /// able to connect to any hosts.
    /// </summary>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
    /// <exception cref="MissingOptionalComponentException"></exception>
    /// <exception cref="VirtualMachinePlatformMissingException"></exception>
    /// <exception cref="UnknownWslErrorCodeException"></exception>
    /// <exception cref="GuacdDistributionMissingException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static void RequestStart() {
        lock (s_lock) {
            // if guacd is already running, do nothing
            if (IsRunning) {
                return;
            }

            // ensure we start with a fresh wait state each time we attempt to start guacd
            s_started.Reset();

            // WSL is required to run guacd
            ConfirmWslIsReady();

            if (!IsGuacdDistributionInstalled) {
                throw new GuacdDistributionMissingException(containerName);
            }

            s_cts = new CancellationTokenSource();
            var token = s_cts.Token;

            s_worker = Task.Run(async () => {
                try {
                    LastException = null; // reset last exception on each start attempt

                    // terminate the wsl distro if it’s already running
                    WriteLogline("[Manager] INFO: Terminating any existing guacd WSL instances...", true);
                    Run(@"C:\Program Files\WSL\wsl.exe", $"--terminate {containerName}");

                    // start the daemon
                    var startInfo = new ProcessStartInfo {
                        FileName = @"C:\Program Files\WSL\wsl.exe",
                        Arguments = $"-d {containerName} " +
                                    $"ash -c \"LOG_LEVEL=info exec /opt/guacamole/entrypoint.sh\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    var guacdProcess = Process.Start(startInfo);
                    if (guacdProcess == null) {
                        throw new InvalidOperationException("Failed to start guacamole daemon.");
                    }

                    // stream the dockerd output to a log file
                    guacdProcess.OutputDataReceived += (_, e) => { if (e.Data != null) WriteLogline(e.Data, false); };
                    guacdProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) WriteLogline(e.Data, true); };
                    guacdProcess.BeginOutputReadLine();
                    guacdProcess.BeginErrorReadLine();

                    // log when the dockerd process exits
                    guacdProcess.Exited += (s, e) => {
                        WriteLogline($"dockerd exited at {DateTime.Now}");
                    };

                    // signal that guacd has started once the service is accepting connections
                    while (!token.IsCancellationRequested) {
                        if (IsGuacdHealthy) {
                            s_started.Set();
                            break;
                        }
                        await Task.Delay(500, token);
                    }

                    // keep waiting until the cancellation token is triggered,
                    // which is the signal that we should stop docker 
                    while (!token.IsCancellationRequested) {
                        await Task.Delay(1000, token);
                    }
                    if (guacdProcess != null && !guacdProcess.HasExited) {
                        guacdProcess.Kill();
                        guacdProcess.Dispose();
                    }
                }
                catch (TaskCanceledException) { }
            }, token);

            s_worker.ContinueWith(t => {
                if (t.IsFaulted && t.Exception is not null) {
                    WriteLogline("[Manager] ERROR: Background task failed: " + t.Exception, true);
                    LastException = t.Exception;
                    s_started.Set(); // unblock WaitUntilRunning early if it’s waiting
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    /// <summary>
    /// Stops the guacd process, cleans up resources, and terminates the WSL distribution.
    /// </summary>
    public static Task Stop() {
        WriteLogline("[Manager] INFO: Stopping guacd...", true);
        return Task.Run(() => {
            lock (s_lock) {
                if (!IsRunning)
                    return;
                s_cts?.Cancel();
                s_cts?.Dispose();
                s_worker = null;
                s_started.Reset();
                TerminateWslDistro();
            }
        });
    }

    /// <summary>
    /// Terminates the guacd WSL distribution by calling "wsl --terminate {containerName}".
    /// </summary>
    private static Task TerminateWslDistro() {
        return Task.Run(() => {
            try {
                WriteLogline("[Manager] INFO: Terminating guacd WSL instance...", true);
                Run(@"C:\Program Files\WSL\wsl.exe", $"--terminate {containerName}");
            }
            catch (Exception ex) {
                WriteLogline("[Manager] ERROR: Failed to terminate guacd WSL instance: " + ex, true);
            }
        });
    }

    /// <summary>
    /// Waits until guacd is running and accepting connections, or until the specified timeout has elapsed.
    /// If guacd fails to start or does not become healthy within the timeout, this method throws an exception.
    /// This should be called after RequestStart to ensure that guacd is fully started before the web server
    /// attempts to connect to it.
    /// </summary>
    /// <param name="timeout"></param>
    /// <exception cref="AggregateException"></exception>
    /// <exception cref="TimeoutException"></exception>
    public static void WaitUntilRunning(TimeSpan timeout) {
        if (!IsRunning) {
            WriteLogline("[Manager] INFO: Waiting for guacd to start...", true);
        }

        s_started.Wait(timeout);

        if (LastException is not null) {
            Stop();
            throw new AggregateException("Guacd failed to start.", LastException);
        }

        if (!IsRunning) {
            Stop();
            throw new TimeoutException("Guacd did not start within the expected time.");
        }

        WriteLogline("[Manager] INFO: Guacd is running.", true);
    }

    /// <summary>
    /// Runs a process with the specified file and arguments and waits for it to exit.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="args"></param>
    private static int Run(string file, string args) {
        var p = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        p.Start();
        p.WaitForExit();
        var exitCode = p.ExitCode;
        p.Dispose();
        return exitCode;
    }

    /// <summary>
    /// Runs a process with the specified file and arguments, waits for it to exit, and returns
    /// the standard output as a string.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="args"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    private static string RunWithOutput(string file, string args, out int exitCode, Encoding? encoding = null) {
        var p = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding ?? Encoding.UTF8,
            }
        };
        using (p) {
            p.Start();
            var output = p.StandardOutput.ReadToEnd();

            // strip null bytes from the output
            output = output.Replace("\0", "");

            p.WaitForExit();
            exitCode = p.ExitCode;
            p.Dispose();
            return output;
        }
    }
}


public class GuacdNotRunningException : Exception {
    public GuacdNotRunningException() : base("Guacamole daemon is not running.") { }
}

public class WindowsSubsystemForLinuxMissingException : Exception {
    public WindowsSubsystemForLinuxMissingException() : base("Windows Subsystem for Linux is not installed.") { }
}

public class GuacdDistributionMissingException : Exception {
    public GuacdDistributionMissingException(string containerName) : base($"Guacd WSL distribution {containerName} is not installed.") { }
}

public class GuacdInstallFailedException : Exception {
    public GuacdInstallFailedException(string message) : base(message) { }
}

public class MissingOptionalComponentException : Exception {
    public MissingOptionalComponentException() : base("Windows Subsystem for Linux requires the Windows Subsystem for Linux optional component to be installed.") { }
}

public class VirtualMachinePlatformMissingException : Exception {
    public VirtualMachinePlatformMissingException() : base("Windows Subsystem for Linux 2 requires the Virtual Machine Platform optional component to be installed.") { }
}

public class VirtualMachinePlatformUnavailableException : Exception {
    public VirtualMachinePlatformUnavailableException() : base("The Virtual Machine Platform is unavailable. Please ensure that the Virtual Machine Platform optional component is enabled and that your hardware supports virtualization.") { }
}

public class UnknownWslErrorCodeException : Exception {
    public string ErrorCode { get; init; }
    public UnknownWslErrorCodeException(string errorCode) : base("An unknown WSL error occurred: " + errorCode) {
        ErrorCode = errorCode;
    }
}
