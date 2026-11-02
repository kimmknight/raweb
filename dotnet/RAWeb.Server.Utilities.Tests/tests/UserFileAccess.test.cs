using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities.Tests;

public class UserFileAccessTests {
  private static readonly SecurityIdentifier s_currentUser = WindowsIdentity.GetCurrent().User!;
  private static readonly SecurityIdentifier s_localSystem = new(WellKnownSidType.LocalSystemSid, null);
  private static readonly SecurityIdentifier s_everyone = new(WellKnownSidType.WorldSid, null);

  /// <summary>
  /// Creates a temporary file, opens it, and then replaces its access rules with the specified
  /// rules (without inherited rules). The file is opened before the rules are replaced so that
  /// the test can check files that the current user cannot open.
  /// </summary>
  private static async Task WithFileWithRules(Func<FileStream, Task> test, params FileSystemAccessRule[] rules) {
    var path = Path.GetTempFileName();
    try {
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

      var security = new FileSecurity();
      security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
      foreach (var rule in rules) {
        security.AddAccessRule(rule);
      }
      new FileInfo(path).SetAccessControl(security);

      await test(stream);
    }
    finally {
      // restore access so that the file can be deleted
      var security = new FileSecurity();
      security.AddAccessRule(new FileSystemAccessRule(s_currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
      new FileInfo(path).SetAccessControl(security);
      File.Delete(path);
    }
  }

  [Test]
  public async Task CanRead_TrueWhenUserIsAllowed() {
    await WithFileWithRules(
      async stream => await Assert.That(UserFileAccess.CanRead(stream, s_currentUser)).IsTrue(),
      new FileSystemAccessRule(s_currentUser, FileSystemRights.Read, AccessControlType.Allow)
    );
  }

  /// <summary>
  /// A user must not be able to read files that only a privileged account can read
  /// (e.g., by setting their wallpaper to that file).
  /// </summary>
  [Test]
  public async Task CanRead_FalseWhenOnlyAnotherAccountIsAllowed() {
    await WithFileWithRules(
      async stream => await Assert.That(UserFileAccess.CanRead(stream, s_currentUser)).IsFalse(),
      new FileSystemAccessRule(s_localSystem, FileSystemRights.FullControl, AccessControlType.Allow)
    );
  }

  [Test]
  public async Task CanRead_TrueWhenAGroupOfTheUserIsAllowed() {
    await WithFileWithRules(
      async stream => await Assert.That(UserFileAccess.CanRead(stream, s_currentUser)).IsTrue(),
      new FileSystemAccessRule(s_everyone, FileSystemRights.Read, AccessControlType.Allow)
    );
  }

  [Test]
  public async Task CanRead_FalseWhenUserIsDenied() {
    await WithFileWithRules(
      async stream => await Assert.That(UserFileAccess.CanRead(stream, s_currentUser)).IsFalse(),
      new FileSystemAccessRule(s_currentUser, FileSystemRights.Read, AccessControlType.Deny),
      new FileSystemAccessRule(s_everyone, FileSystemRights.Read, AccessControlType.Allow)
    );
  }

  /// <summary>
  /// Owners can always read and change a file's permissions, but that must not count as reading its data.
  /// </summary>
  [Test]
  public async Task CanRead_FalseForOwnerWithoutReadDataAccess() {
    await WithFileWithRules(
      async stream => await Assert.That(UserFileAccess.CanRead(stream, s_currentUser)).IsFalse(),
      new FileSystemAccessRule(s_currentUser, FileSystemRights.ReadPermissions, AccessControlType.Allow)
    );
  }
}
