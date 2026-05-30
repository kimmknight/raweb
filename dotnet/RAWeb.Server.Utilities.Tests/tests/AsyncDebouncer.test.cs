
namespace RAWeb.Server.Utilities.Tests;

public class AsyncDebouncerTests {
  [Test]
  public async Task OnlyExecutesOnceWhenCalledRepeatedly() {
    var debouncer = new AsyncDebouncer();

    // track the number of times a task is executed
    var callCount = 0;

    var tasks = new List<Task>();
    for (var i = 0; i < 5; i++) {
      tasks.Add(debouncer.DebounceAsync(50, () => {
        callCount++;
        return Task.CompletedTask;
      }));
    }
    await Task.WhenAll(tasks);

    await Assert.That(callCount).IsEqualTo(1);
  }

  [Test]
  public async Task ExecutesAfterDelay() {
    var debouncer = new AsyncDebouncer();
    var executed = false;

    var startTime = DateTime.UtcNow;
    var elapsedTime = TimeSpan.Zero;

    await debouncer.DebounceAsync(50, () => {
      executed = true;
      elapsedTime = DateTime.UtcNow - startTime;
      return Task.CompletedTask;
    });

    await Assert.That(executed).IsTrue();
    await Assert.That(elapsedTime.TotalMilliseconds).IsGreaterThanOrEqualTo(50);
  }

  [Test]
  public async Task ExecutesLatestActionWhenCalledMultipleTimes() {
    var debouncer = new AsyncDebouncer();
    var lastExecuted = 0;

    var t1 = debouncer.DebounceAsync(50, () => {
      lastExecuted = 1;
      return Task.CompletedTask;
    });
    var t2 = debouncer.DebounceAsync(50, () => {
      lastExecuted = 2;
      return Task.CompletedTask;
    });
    await Task.WhenAll(t1, t2);

    await Assert.That(lastExecuted).IsEqualTo(2);
  }
}
