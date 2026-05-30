namespace RAWeb.Server.Utilities.Tests;

// NotInParallel because SetPolicy mutates the static PoliciesManager.s_appSettingsPath.
[NotInParallel]
public class LoggerTests {
  private string _tempConfigPath = "";
  private string _id = "";

  [Before(Test)]
  public void Setup() {
    _tempConfigPath = Path.GetTempFileName();
    SetPolicy(null);
    _id = "testlog-" + Guid.NewGuid().ToString("N")[..8];
    Directory.CreateDirectory(Path.Combine(Constants.AppDataFolderPath, "logs"));
  }

  [After(Test)]
  public void Cleanup() {
    CleanupLoggerFiles(_id);
    if (File.Exists(_tempConfigPath)) {
      File.Delete(_tempConfigPath);
    }
  }

  private void SetPolicy(string? value) {
    var xml = value is null
      ? "<configuration><appSettings></appSettings></configuration>"
      : $"<configuration><appSettings><add key=\"LogFiles.DiscardAgeDays\" value=\"{value}\" /></appSettings></configuration>";
    File.WriteAllText(_tempConfigPath, xml);
    new PoliciesManager(_tempConfigPath);
  }

  private static void CleanupLoggerFiles(string id) {
    var logsDir = Path.Combine(Constants.AppDataFolderPath, "logs");
    if (!Directory.Exists(logsDir)) {
      return;
    }
    foreach (var file in Directory.GetFiles(logsDir, $"{id}_*.log")) {
      try { File.Delete(file); } catch { }
    }
  }

  private static string GetLogFilePath(string id, DateTime? date = null) {
    var d = (date ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
    return Path.Combine(Constants.AppDataFolderPath, "logs", $"{id}_{d}.log");
  }

  /// <summary>
  /// Logger writes are processed on a background queue, so tests must poll rather than assert immediately.
  /// </summary>
  /// <param name="path"></param>
  /// <param name="expectedContent"></param>
  /// <param name="timeoutMs"></param>
  /// <returns></returns>
  private static async Task<bool> WaitForFileContentAsync(string path, string expectedContent, int timeoutMs = 3000) {
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < deadline) {
      if (File.Exists(path)) {
        var content = File.ReadAllText(path);
        if (content.Contains(expectedContent)) {
          return true;
        }
      }
      await Task.Delay(20);
    }
    return false;
  }

  [Test]
  public async Task Constructor_ReplacesUnderscoresWithDashesInId() {
    var logger = new Logger("my_logger_id");
    logger.Dispose();

    await Assert.That(logger.Id).IsEqualTo("my-logger-id");
  }

  [Test]
  public async Task Constructor_ReplacesInvalidFileNameCharsInId() {
    var logger = new Logger("my:log>ger");
    logger.Dispose();

    await Assert.That(logger.Id.Contains(':')).IsFalse();
    await Assert.That(logger.Id.Contains('>')).IsFalse();
  }

  [Test]
  public async Task Constructor_PreservesValidCharsInId() {
    var logger = new Logger("mylogger");
    logger.Dispose();

    await Assert.That(logger.Id).IsEqualTo("mylogger");
  }

  [Test]
  public async Task Dispose_IsIdempotent() {
    var logger = new Logger(_id);
    logger.Dispose();
    logger.Dispose();
  }

  [Test]
  public async Task WriteLogline_IsNoOpAfterDispose() {
    var logger = new Logger(_id);
    logger.Dispose();

    logger.WriteLogline("discarded");

    await Task.Delay(100);
    await Assert.That(File.Exists(GetLogFilePath(_id))).IsFalse();
  }

  [Test]
  public async Task WriteLogline_CreatesFileWithCorrectNameFormat() {
    var expectedPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("hello");
    var found = await WaitForFileContentAsync(expectedPath, "hello");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(expectedPath)).IsTrue();
  }

  [Test]
  public async Task WriteLogline_PrefixesLineWithIso8601Timestamp() {
    var expectedPath = GetLogFilePath(_id);
    var datePart = DateTime.UtcNow.ToString("yyyy-MM-dd");

    var logger = new Logger(_id);
    logger.WriteLogline("my-marker");
    var found = await WaitForFileContentAsync(expectedPath, "my-marker");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    var content = File.ReadAllText(expectedPath);
    // format: [yyyy-MM-ddTHH:mm:ssZ] my-marker
    await Assert.That(content.Contains($"[{datePart}T")).IsTrue();
    await Assert.That(content.Contains("Z] my-marker")).IsTrue();
  }

  [Test]
  public async Task WriteLogline_AppendsNewlineAfterEachEntry() {
    var expectedPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("line-one");
    logger.WriteLogline("line-two");
    var found = await WaitForFileContentAsync(expectedPath, "line-two");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    var content = File.ReadAllText(expectedPath);
    await Assert.That(content.Contains("line-one")).IsTrue();
    await Assert.That(content.Contains("line-two")).IsTrue();
    // each line ends with a newline
    await Assert.That(content.Contains("line-one" + Environment.NewLine)).IsTrue();
  }

  [Test]
  public async Task WriteLogline_WritesMultipleLinesToSameFile() {
    var expectedPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("alpha");
    logger.WriteLogline("beta");
    logger.WriteLogline("gamma");
    var found = await WaitForFileContentAsync(expectedPath, "gamma");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    var content = File.ReadAllText(expectedPath);
    await Assert.That(content.Contains("alpha")).IsTrue();
    await Assert.That(content.Contains("beta")).IsTrue();
    await Assert.That(content.Contains("gamma")).IsTrue();
  }

  [Test]
  public async Task WriteLogline_DoesNotWriteFileWhenRetentionDisabled() {
    SetPolicy("false");
    var expectedPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("should-not-appear");
    await Task.Delay(200);
    logger.Dispose();

    await Assert.That(File.Exists(expectedPath)).IsFalse();
  }

  [Test]
  public async Task WriteLogline_DeletesFilesOlderThanExplicitRetentionDays() {
    // Deletion is triggered inside logPath getter when today's file doesn't exist yet.
    // Writing any log line causes that getter to run for the first time, which fires cleanup.

    SetPolicy("3");
    var oldPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-4));
    File.WriteAllText(oldPath, "old content");
    var todayPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("trigger");
    var found = await WaitForFileContentAsync(todayPath, "trigger");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(oldPath)).IsFalse();
  }

  [Test]
  public async Task WriteLogline_KeepsFilesWithinExplicitRetentionDays() {
    SetPolicy("3");
    var recentPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-1));
    File.WriteAllText(recentPath, "recent content");
    var todayPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("trigger");
    var found = await WaitForFileContentAsync(todayPath, "trigger");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(recentPath)).IsTrue();
  }

  [Test]
  public async Task WriteLogline_DeletesFilesOlderThan3DaysWhenNoPolicyIsSet() {
    SetPolicy(null);
    var oldPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-4));
    File.WriteAllText(oldPath, "old content");
    var todayPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("trigger");
    var found = await WaitForFileContentAsync(todayPath, "trigger");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(oldPath)).IsFalse();
  }

  [Test]
  public async Task WriteLogline_DiscardFilesAtExactRetentionBoundary() {
    // A file exactly at the boundary (in this case: 3 days ago) should be biscarded
    SetPolicy("3");
    var boundaryPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-3));
    File.WriteAllText(boundaryPath, "boundary content");
    var todayPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("trigger");
    var found = await WaitForFileContentAsync(todayPath, "trigger");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(boundaryPath)).IsFalse();
  }

  [Test]
  public async Task WriteLogline_UsesCustomRetentionDaysFromPolicy() {
    SetPolicy("7");
    // 5 days old: within 7-day window; should be kept
    var recentPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-5));
    File.WriteAllText(recentPath, "recent content");
    // 8 days old: beyond 7-day window; should be deleted
    var oldPath = GetLogFilePath(_id, DateTime.UtcNow.AddDays(-8));
    File.WriteAllText(oldPath, "old content");
    var todayPath = GetLogFilePath(_id);

    var logger = new Logger(_id);
    logger.WriteLogline("trigger");
    var found = await WaitForFileContentAsync(todayPath, "trigger");
    logger.Dispose();

    await Assert.That(found).IsTrue();
    await Assert.That(File.Exists(recentPath)).IsTrue();
    await Assert.That(File.Exists(oldPath)).IsFalse();
  }

  [Test]
  public async Task WriteLogline_OnlyDeletesFilesMatchingOwnId() {
    SetPolicy("3");
    var otherId = "testlog-other-" + Guid.NewGuid().ToString("N")[..8];
    var otherOldPath = GetLogFilePath(otherId, DateTime.UtcNow.AddDays(-4));
    try {
      File.WriteAllText(otherOldPath, "other logger content");
      var todayPath = GetLogFilePath(_id);

      var logger = new Logger(_id);
      logger.WriteLogline("trigger");
      var found = await WaitForFileContentAsync(todayPath, "trigger");
      logger.Dispose();

      await Assert.That(found).IsTrue();
      await Assert.That(File.Exists(otherOldPath)).IsTrue();
    }
    finally {
      if (File.Exists(otherOldPath)) {
        File.Delete(otherOldPath);
      }
    }
  }
}
