using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAWeb.Server.Utilities;

public class Logger {
    public string Id { get; }

    /// <summary>
    /// The path to the guacd-tunnel log file. Guacd-tunnel logs are written to a daily log file in the App_Data/logs folder.
    /// </summary>
    private string logPath {
        get {
            var isoDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var logFileName = $"{Id}-{isoDate}.log";
            var logFilePath = Path.Combine(Constants.AppDataFolderPath, "logs", logFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            return logFilePath;
        }
    }

    public void WriteLogline(string line) {
        var iso8601Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        _logQueue.Add($"[{iso8601Timestamp}] {line}");
    }

    /// <summary>
    /// A queue for log lines that will be written to the guacd log file.
    /// </summary>
    private readonly BlockingCollection<string> _logQueue = [];

    public Logger(string id) {
        Id = id;

        // start a background task to write log lines to the log file
        Task.Run(() => {
            try {
                foreach (var line in _logQueue.GetConsumingEnumerable()) {
                    // try to write at least three times in case the file is locked
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
                Console.Error.WriteLine($"{Id} log writer failed: " + ex);
            }
        });
    }

}
