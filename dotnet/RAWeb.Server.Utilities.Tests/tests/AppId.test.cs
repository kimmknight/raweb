namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class AppIdTests {
  private string _tempTestRootPath = null!;

  [Before(Test)]
  public void SetupTempAppDataPath() {
    _tempTestRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempTestRootPath);
    Constants.AppRoot = _tempTestRootPath;
    Console.WriteLine($"Using temporary AppRoot: {Constants.AppRoot}");
  }

  [After(Test)]
  public void CleanupTempAppDataPath() {
    if (Directory.Exists(_tempTestRootPath)) {
      Directory.Delete(_tempTestRootPath, true);
      Console.WriteLine($"Deleted temporary AppRoot: {Constants.AppRoot}");
    }
  }

  [Test]
  public async Task Initialize_CreatesAppIdFileIfNoneExists() {
    AppId.Initialize();

    var appIdFiles = Directory.GetFiles(Constants.AppDataFolderPath, "*.appid");
    Console.WriteLine(Constants.AppDataFolderPath);
    await Assert.That(appIdFiles.Length).IsEqualTo(1);
  }

  [Test]
  public async Task Initialize_ThrowsExceptionIfMultipleAppIdFilesExist() {
    var appIdFile1 = Path.Combine(Constants.AppDataFolderPath, Guid.NewGuid().ToString() + ".appid");
    var appIdFile2 = Path.Combine(Constants.AppDataFolderPath, Guid.NewGuid().ToString() + ".appid");
    File.WriteAllText(appIdFile1, "dummy content");
    File.WriteAllText(appIdFile2, "dummy content");

    await Assert.ThrowsAsync<TooManyAppIdFilesException>(() => Task.Run(() => AppId.Initialize()));
  }

  [Test]
  public async Task ToGuid_ReturnsCorrectGuidFromAppIdFile() {
    var expectedGuid = Guid.NewGuid();
    var appIdFile = Path.Combine(Constants.AppDataFolderPath, expectedGuid.ToString() + ".appid");
    File.WriteAllText(appIdFile, "dummy content");

    var actualGuid = AppId.ToGuid();

    await Assert.That(actualGuid).IsEqualTo(expectedGuid);
  }

  [Test]
  public async Task ToGuid_ThrowsExceptionIfNoAppIdFileExists() {
    // Try to get GUID without initializing, which should result in no appid file existing
    // since we use a new temporary app data path for each test
    await Assert.ThrowsAsync<NoAppIdFileFoundException>(() => Task.Run(() => AppId.ToGuid()));
  }
}
