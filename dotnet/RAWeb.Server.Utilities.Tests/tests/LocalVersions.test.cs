namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class LocalVersionsTests {
  [Test]
  public async Task GetServerVersionString_ReturnsNonNullString() {
    var version = LocalVersions.GetServerVersionString();

    await Assert.That(version).IsNotNull();
  }

  [Test]
  public async Task GetServerVersionString_ReturnsFallbackWhenNoFileVersionAttribute() {
    // The test assembly has no AssemblyFileVersionAttribute, so it falls back to "1.0.0.0"
    var version = LocalVersions.GetServerVersionString();

    await Assert.That(version).IsEqualTo("1.0.0.0");
  }

  [Test]
  public async Task GetWebClientVersionString_ReturnsTimestampFileContent() {
    var version = LocalVersions.GetWebClientVersionString();

    await Assert.That(version).IsEqualTo("2026.06.04.0");
  }
}
