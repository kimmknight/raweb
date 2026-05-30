namespace RAWeb.Server.Utilities.Tests;

public class ConcurrentHashSetTests {
  [Test]
  public async Task TryAdd_ReturnsTrueForNewItem() {
    var set = new ConcurrentHashSet<string>();

    var result = set.TryAdd("item");

    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task TryAdd_ReturnsFalseForDuplicateItem() {
    var set = new ConcurrentHashSet<string>();
    set.TryAdd("item");

    var result = set.TryAdd("item");

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task TryRemove_ReturnsTrueForExistingItem() {
    var set = new ConcurrentHashSet<string>();
    set.TryAdd("item");

    var result = set.TryRemove("item");

    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task TryRemove_ReturnsFalseForMissingItem() {
    var set = new ConcurrentHashSet<string>();

    var result = set.TryRemove("item");

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task Contains_ReturnsTrueForExistingItem() {
    var set = new ConcurrentHashSet<string>();
    set.TryAdd("item");

    await Assert.That(set.Contains("item")).IsTrue();
  }

  [Test]
  public async Task Contains_ReturnsFalseForMissingItem() {
    var set = new ConcurrentHashSet<string>();

    await Assert.That(set.Contains("item")).IsFalse();
  }

  [Test]
  public async Task Count_ReflectsAddAndRemove() {
    var set = new ConcurrentHashSet<string>();
    set.TryAdd("a");
    set.TryAdd("b");

    await Assert.That(set.Count).IsEqualTo(2);

    set.TryRemove("a");

    await Assert.That(set.Count).IsEqualTo(1);
  }

  [Test]
  public async Task GetEnumerator_YieldsAllItems() {
    var set = new ConcurrentHashSet<string>();
    set.TryAdd("a");
    set.TryAdd("b");
    set.TryAdd("c");

    var items = new List<string>();
    foreach (var item in set) {
      items.Add(item);
    }

    await Assert.That(items.Count).IsEqualTo(3);
    await Assert.That(items.Contains("a")).IsTrue();
    await Assert.That(items.Contains("b")).IsTrue();
    await Assert.That(items.Contains("c")).IsTrue();
  }
}
