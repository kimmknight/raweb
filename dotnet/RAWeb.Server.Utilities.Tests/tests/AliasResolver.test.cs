namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class AliasResolverTests {
  private string _tempFile = null!;

  [Before(Test)]
  public void SetupTempFile() {
    _tempFile = Path.GetTempFileName();
    File.Delete(_tempFile);
    _ = new PoliciesManager(_tempFile);
  }

  [After(Test)]
  public void CleanupTempFile() {
    if (File.Exists(_tempFile)) {
      File.Delete(_tempFile);
    }
  }

  [Test]
  public async Task Resolve_ReturnsUnchangedNameWhenNoAliasesConfigured() {
    var resolver = new AliasResolver();

    var result = resolver.Resolve("myserver");

    await Assert.That(result).IsEqualTo("myserver");
  }

  [Test]
  public async Task Resolve_ReturnsAliasForKnownName() {
    PoliciesManager.Set("TerminalServerAliases", "myserver=My Server Alias");
    var resolver = new AliasResolver();

    var result = resolver.Resolve("myserver");

    await Assert.That(result).IsEqualTo("My Server Alias");
  }

  [Test]
  public async Task Resolve_ReturnsUnchangedNameForUnknownName() {
    PoliciesManager.Set("TerminalServerAliases", "myserver=My Server Alias");
    var resolver = new AliasResolver();

    var result = resolver.Resolve("otherserver");

    await Assert.That(result).IsEqualTo("otherserver");
  }

  [Test]
  public async Task Resolve_ParsesMultipleAliasesSeparatedBySemicolons() {
    PoliciesManager.Set("TerminalServerAliases", "server1=Alias One;server2=Alias Two");
    var resolver = new AliasResolver();

    await Assert.That(resolver.Resolve("server1")).IsEqualTo("Alias One");
    await Assert.That(resolver.Resolve("server2")).IsEqualTo("Alias Two");
  }

  [Test]
  public async Task Resolve_TrimsWhitespaceFromNamesAndAliases() {
    PoliciesManager.Set("TerminalServerAliases", " server1 = Alias One ");
    var resolver = new AliasResolver();

    var result = resolver.Resolve("server1");

    await Assert.That(result).IsEqualTo("Alias One");
  }

  [Test]
  public async Task Resolve_TrimsWhitespaceFromMultipleAliases() {
    PoliciesManager.Set("TerminalServerAliases", "server1=   Alias One; server2 = Alias Two");
    var resolver = new AliasResolver();

    await Assert.That(resolver.Resolve("server1")).IsEqualTo("Alias One");
    await Assert.That(resolver.Resolve("server2")).IsEqualTo("Alias Two");
  }

  [Test]
  public async Task Resolve_UsesFirstDefinitionForDuplicateKeys() {
    PoliciesManager.Set("TerminalServerAliases", "server1=First Alias;server1=Second Alias");
    var resolver = new AliasResolver();

    var result = resolver.Resolve("server1");

    await Assert.That(result).IsEqualTo("First Alias");
  }
}
