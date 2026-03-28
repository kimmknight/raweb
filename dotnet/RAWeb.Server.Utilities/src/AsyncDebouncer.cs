using System;
using System.Threading;
using System.Threading.Tasks;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Provides debouncing for asynchronous actions.
/// 
/// An AsyncDebouncer ensures that a specified asynchronous action
/// is only executed after a certain period of inactivity. If the
/// DebounceAsync method is called again before the delay period
/// elapses, the previous call is canceled and the delay timer resets.
/// 
/// If a debouncer is used for multiple different actions,
/// the latest action provided to DebounceAsync will be the one executed
/// after the delay period.
/// </summary>
public sealed class AsyncDebouncer {
  private CancellationTokenSource? _cts;
  private readonly object _lock = new();

  public async Task DebounceAsync(int delayMs, Func<Task> action) {
    CancellationTokenSource cts;

    lock (_lock) {
      if (_cts != null) {
        _cts.Cancel();
        _cts.Dispose();
      }

      _cts = new CancellationTokenSource();
      cts = _cts;
    }

    try {
      // wait for debounce window
      await Task.Delay(delayMs, cts.Token);

      // if not canceled, execute the action
      if (!cts.IsCancellationRequested) {
        await action();
      }
    }
    catch (OperationCanceledException) {
      // expected when debounce resets
    }
  }
}
