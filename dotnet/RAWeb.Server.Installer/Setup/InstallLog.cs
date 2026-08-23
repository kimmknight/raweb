namespace RAWeb.Server.Installer.Setup;

public enum LogSeverity {
  Detail,
  Step,
  Info,
  Success,
  Warning,
  Error,
}

public sealed record LogEntry(LogSeverity Severity, string Message, DateTime TimestampUtc) {
  public override string ToString() => Message;
}

public sealed record ProgressUpdate(int CompletedSteps, int TotalSteps, string CurrentStep) {
  /// <summary>
  /// Fraction complete in the range 0-100, or -1 when the current work is indeterminate.
  /// </summary>
  public double Percent => TotalSteps <= 0 ? -1 : Math.Min(100d, CompletedSteps * 100d / TotalSteps);
}

/// <summary>
/// Presents messages in the log view of the install wizard.
/// </summary>
public sealed class InstallLog {
  public event Action<LogEntry>? Logged;
  public event Action<ProgressUpdate>? ProgressChanged;

  private readonly List<LogEntry> _entries = [];

  public IReadOnlyList<LogEntry> Entries {
    get {
      lock (_entries) {
        return [.. _entries];
      }
    }
  }

  public void Write(LogSeverity severity, string message) {
    var entry = new LogEntry(severity, message, DateTime.UtcNow);
    lock (_entries) { _entries.Add(entry); }
    Logged?.Invoke(entry);
  }

  public void Detail(string message) => Write(LogSeverity.Detail, "  " + message);
  public void Info(string message) => Write(LogSeverity.Info, message);
  public void Success(string message) => Write(LogSeverity.Success, message);
  public void Warning(string message) => Write(LogSeverity.Warning, message);
  public void Error(string message) => Write(LogSeverity.Error, message);

  public void Step(int completed, int total, string title) {
    Write(LogSeverity.Step, $"[{completed}/{total}] {title}");
    ProgressChanged?.Invoke(new ProgressUpdate(completed - 1, total, title));
  }

  public void Indeterminate(string currentStep) => ProgressChanged?.Invoke(new ProgressUpdate(0, 0, currentStep));
}
