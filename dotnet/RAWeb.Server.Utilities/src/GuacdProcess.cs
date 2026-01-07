using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWeb.Server.Utilities;

public static class Guacd {
    private static string containerName => $"guacd-{AppId.ToGuid()}";
    private static readonly object s_lock = new();
    private static Task? s_worker;
    private static CancellationTokenSource? s_cts;
    private static readonly ManualResetEventSlim s_started = new();

    /// <summary>
    /// The path to the guacd log file. Guacd logs are written to a daily log file in the App_Data/logs folder.
    /// </summary>
    private static string logPath {
        get {
            var isoDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var logFileName = $"guacd-{isoDate}.log";
            var logFilePath = Path.Combine(Constants.AppDataFolderPath, "logs", logFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            return logFilePath;
        }
    }

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
            return RunWithOutput(
                @"C:\Program Files\WSL\wsl.exe",
                $"-d {containerName} -- sh -c \"ip addr show eth0 | sed -n 's/.*inet \\([0-9.]*\\)\\/.*/\\1/p'\""
            ).Trim();
        }
    }

    /// <summary>
    /// A queue for log lines that will be written to the guacd log file.
    /// </summary>
    private static readonly BlockingCollection<string> s_logQueue = [];

    // start a background task to write log lines to the log file
    private static readonly Task s_logWriter = Task.Run(() => {
        try {
            foreach (var line in s_logQueue.GetConsumingEnumerable()) {
                // try to write at least three times in case the file is locked
                // by another process
                var written = false;
                for (var i = 0; i < 3 && !written; i++) {
                    try {
                        File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
                        written = true;
                    }
                    catch (IOException) {
                        Thread.Sleep(50);
                    }
                }
            }
        }
        catch (ThreadAbortException) {
        }
        catch (TaskCanceledException) { }
        catch (Exception ex) {
            Console.Error.WriteLine("Guacd log writer failed: " + ex);
        }
    });

    private static void WriteLogline(string line) {
        var iso8601Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

        // remove preceding occurences of "guacd[pid]: "
        var cleanedLine = System.Text.RegularExpressions.Regex.Replace(line, @"^guacd\[\d+\]:\s*", "");

        s_logQueue.Add($"[{iso8601Timestamp}] {cleanedLine}");
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
    /// Checks whether WSL is installed to C:\Program Files\WSL\wsl.exe.
    /// </summary>
    private static bool IsWindowsSubsystemForLinuxInstalled => File.Exists(@"C:\Program Files\WSL\wsl.exe");

    /// <summary>
    /// Checks whether the guacd WSL distribution is installed.
    /// This checks the names of the installed WSL distributions for a distribution named "guacd-{appId}",
    /// where {appId} is the id of the current RAWeb installation.
    /// </summary>
    public static bool IsGuacdDistributionInstalled {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return false;
            }
            foreach (var distro in AllInstalledDistriubtions) {
                if (distro == containerName) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the names of all installed WSL distributions by running "wsl --list --quiet" and
    /// parsing the output.
    /// </summary>
    private static string[] AllInstalledDistriubtions {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return [];
            }
            var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", "--list --quiet", Encoding.Unicode);
            var distros = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            return distros;
        }
    }

    /// <summary>
    /// Checks whether the guacd WSL distribution is currently running.
    /// This checks the names of the running WSL distributions for a distribution
    /// named "guacd-{appId}",
    /// </summary>
    private static bool IsGuacdDistributionRunning {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return false;
            }
            foreach (var distro in AllRunningDistriubtions) {
                if (distro.Trim() == containerName) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the names of all running WSL distributions by running "wsl --list --quiet --running"
    /// and parsing the output.
    /// </summary>
    private static string[] AllRunningDistriubtions {
        get {
            if (!IsWindowsSubsystemForLinuxInstalled) {
                return [];
            }
            var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", "--list --quiet --running", Encoding.Unicode);
            var distros = output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            return distros;
        }
    }

    /// <summary>
    /// Checks whether guacd is healthy. Checks whether the quacd port is accepting connections.
    /// If guacd crashed (orm never fully started), the port will not respond to connections.
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
    /// <exception cref="FileNotFoundException"></exception>
    public static string? InstallGuacd() {
        if (!IsWindowsSubsystemForLinuxInstalled) {
            throw new WindowsSubsystemForLinuxMissingException();
        }

        if (IsGuacdDistributionInstalled) {
            return null;
        }

        var imagePath = Path.Combine(Constants.AppRoot, "bin", $"guacd.wsl");
        if (!File.Exists(imagePath)) {
            throw new FileNotFoundException($"Guacd wsl image not found at {imagePath}");
        }

        // import the guacd distribution from the tarball using wsl --import
        var installLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RAWeb", "containers", containerName);
        Directory.CreateDirectory(installLocation);

        var output = UninstallGuacd();
        output += RunWithOutput(@"C:\Program Files\WSL\wsl.exe", $"--import {containerName} {installLocation} {imagePath} --version 2");
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

        var output = RunWithOutput(@"C:\Program Files\WSL\wsl.exe", $"--unregister {containerName}");
        return output;
    }

    /// <summary>
    /// Attempts to start guacd in a WSL distribution. If guacd is already running, this method does nothing.
    /// If the WSL distribution is not present or fails to start, this method throws an exception. Note that
    /// guacd is required for the web client to function, so if this method fails, the web client will not be able to connect to any hosts.
    /// </summary>
    /// <exception cref="WindowsSubsystemForLinuxMissingException"></exception>
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
            if (!IsWindowsSubsystemForLinuxInstalled) {
                throw new WindowsSubsystemForLinuxMissingException();
            }

            if (!IsGuacdDistributionInstalled) {
                throw new GuacdDistributionMissingException(containerName);
            }

            s_cts = new CancellationTokenSource();
            var token = s_cts.Token;

            s_worker = Task.Run(async () => {
                try {
                    LastException = null; // reset last exception on each start attempt

                    // terminate the wsl distro if it’s already running
                    Console.WriteLine("Guacd: Terminating any existing guacd WSL instances...");
                    Run(@"C:\Program Files\WSL\wsl.exe", $"--terminate {containerName}");

                    // start the daemon
                    Console.WriteLine("Guacd: Starting guacd WSL instance...");
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
                    guacdProcess.OutputDataReceived += (_, e) => { if (e.Data != null) WriteLogline(e.Data); Console.WriteLine(e.Data); };
                    guacdProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) WriteLogline(e.Data); Console.WriteLine(e.Data); };
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
                    Console.Error.WriteLine("Guacd: Background task failed: " + t.Exception);
                    LastException = t.Exception;
                    s_started.Set(); // unblock WaitUntilRunning early if it’s waiting
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    /// <summary>
    /// Stops the guacd process.
    /// </summary>
    public static Task Stop() {
        Console.WriteLine("Guacd: Stopping guacd...");
        return Task.Run(() => {
            lock (s_lock) {
                if (!IsRunning)
                    return;
                s_cts?.Cancel();
                s_worker = null;
                s_started.Reset();
                TerminateWslDistro();
            }
        });
    }

    /// <summary>
    /// Terminates the guacd WSL distribution by calling "wsl --terminate {containerName}".
    /// </summary>
    /// <returns></returns>
    public static Task TerminateWslDistro() {
        return Task.Run(() => {
            try {
                Console.WriteLine("Guacd: Terminating guacd WSL instance...");
                Run(@"C:\Program Files\WSL\wsl.exe", $"--terminate {containerName}");
            }
            catch (Exception ex) {
                Console.Error.WriteLine("Guacd: Failed to terminate guacd WSL instance: " + ex);
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
            Console.WriteLine("Guacd: Waiting for guacd to start...");
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

        Console.WriteLine("Guacd: Guacd is running.");
    }

    /// <summary>
    /// Runs a process with the specified file and arguments and waits for it to exit.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="args"></param>
    private static void Run(string file, string args) {
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
    }

    /// <summary>
    /// Runs a process with the specified file and arguments, waits for it to exit, and returns
    /// the standard output as a string.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="args"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    private static string RunWithOutput(string file, string args, Encoding? encoding = null) {
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
