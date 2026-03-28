using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWeb.Server.Utilities;

public class Logger {
    public string Id { get; }
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    /// <summary>
    /// The path to the guacd-tunnel log file. Guacd-tunnel logs are written to a daily log file in the App_Data/logs folder.
    /// </summary>
    private string logPath {
        get {
            var isoDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var logFileName = $"{Id}_{isoDate}.log";
            var logFilePath = Path.Combine(Constants.AppDataFolderPath, "logs", logFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

            // if the log file is a new day, delete old log files
            if (!File.Exists(logFilePath)) {
                var policy = PoliciesManager.RawPolicies["LogFiles.DiscardAgeDays"];
                if (int.TryParse(policy, out var maxAgeDays) && maxAgeDays > 0) {
                    DeleteOldLogFiles(maxAgeDays);
                }
                else if (policy != "false") {
                    DeleteOldLogFiles(3);
                }
            }

            return logFilePath;
        }
    }

    public void WriteLogline(string line, bool writeToConsole = false) {
        var iso8601Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        if (!_disposed && !_logQueue.IsAddingCompleted && !Disabled) {
            _logQueue.Add($"[{iso8601Timestamp}] {line}");
        }
        if (writeToConsole) {
            Console.WriteLine($"[{Id}] {line}");
        }
    }

    /// <summary>
    /// Indicates whether log retention is disabled via policy.
    /// </summary>
    private static bool Disabled {
        get {
            var policy = PoliciesManager.RawPolicies["LogFiles.DiscardAgeDays"];
            return policy == "false";
        }
    }

    /// <summary>
    /// A queue for log lines that will be written to the guacd log file.
    /// </summary>
    private readonly BlockingCollection<string> _logQueue = [];

    public Logger(string id) {
        // prohibit underscores since we use them to separate the ID and date in log filenames
        id = id.Replace("_", "-");

        // block additional special characters that are not allowed in filenames
        foreach (var c in Path.GetInvalidFileNameChars()) {
            id = id.Replace(c, '-');
        }

        Id = id;

        // start a background task to write log lines to the log file
        Task.Run(async () => {
            try {
                foreach (var line in _logQueue.GetConsumingEnumerable(_cts.Token)) {
                    // try to write at least three times in case the file is locked
                    var written = false;
                    for (var i = 0; i < 3 && !written; i++) {
                        try {
                            File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
                            written = true;
                        }
                        catch (IOException) {
                            await Task.Delay(50, _cts.Token);
                        }
                    }
                }
            }
            catch (ThreadAbortException) {
                // expected when RAWeb is shutting down
            }
            catch (OperationCanceledException) {
                // expected when _logQueue is disposed
                // or when RAWeb is shutting down
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"{Id} log writer failed: " + ex);
            }
        });
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }
        _disposed = true;
        _logQueue.CompleteAdding();
        _cts.Cancel();
        _cts.Dispose();
        _logQueue.Dispose();
    }


    /// <summary>
    /// Deletes log files older than the specified number of days.
    /// </summary>
    /// <param name="maxAgeDays"></param>
    private void DeleteOldLogFiles(int maxAgeDays) {
        try {
            var logDirectory = Path.Combine(Constants.AppDataFolderPath, "logs");
            if (!Directory.Exists(logDirectory)) {
                return;
            }

            var logFiles = Directory.GetFiles(logDirectory, $"{Id}_*.log");
            var thresholdDate = DateTime.UtcNow.AddDays(-maxAgeDays);

            foreach (var logFile in logFiles) {
                var fileName = Path.GetFileName(logFile);

                // delete files without a valid date part
                if (fileName.Length < Id.Length + 15) {
                    File.Delete(logFile);
                    continue;
                }

                var datePart = fileName.Substring(Id.Length + 1, 10); // extract the YYYY-MM-DD part
                if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var logDate) && logDate < thresholdDate) {
                    File.Delete(logFile);
                }
            }
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"{Id} failed to delete old log files: " + ex);
        }
    }
}
