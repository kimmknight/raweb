namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Undo steps registered as the install progresses, unwound last-in-first-out
/// when a step throws. A failing undo step is logged and skipped so the remaining
/// steps still run.
/// </summary>
public sealed class RollbackStack(InstallLog log) {
  private readonly Stack<(string Description, Action Undo)> _steps = new();

  public int Count => _steps.Count;

  public void Push(string description, Action undo) => _steps.Push((description, undo));

  /// <summary>
  /// Returns true when there was anything to roll back.
  /// </summary>
  public bool Unwind() {
    if (_steps.Count == 0) {
      return false;
    }

    log.Warning("Rolling back changes...");


    var total = _steps.Count;
    var completed = 0;

    while (_steps.Count > 0) {
      var (description, undo) = _steps.Pop();
      completed++;
      log.Step(completed, total, description); // show progress for step completion

      try {
        undo();
      }
      catch (Exception exception) {
        log.Warning($"  Rollback step failed: {exception.Message}");
      }
    }

    return true;
  }

  /// <summary>
  /// Discards the undo steps once the install has committed successfully.
  /// </summary>
  public void Commit() => _steps.Clear();
}

public sealed class InstallFailedException(string message, Exception? innerException = null) : Exception(message, innerException);
