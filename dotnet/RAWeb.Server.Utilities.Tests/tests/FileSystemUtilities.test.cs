using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class FileAccessInfoTests {
  private static UserInformation MakeUser(
      string sid = "S-1-5-21-1000",
      string username = "jdoe",
      string domain = "DOMAIN",
      GroupInformation[]? groups = null
  ) => new(sid, username, domain, null, groups ?? []);

  private string _tempTestRootPath = null!;

  [Before(Test)]
  public void SetUpTempAppRoot() {
    // set temporary AppRoot location so that the App_Data folder is isolated for each test
    _tempTestRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(_tempTestRootPath);
    Constants.AppRoot = _tempTestRootPath;
  }

  [After(Test)]
  public void CleanUpTempAppRoot() {
    if (Directory.Exists(_tempTestRootPath)) {
      Directory.Delete(_tempTestRootPath, true);
    }
  }

  // Gets the full path to a location inside the temporary App_Data folder.
  private static string AppDataPath(string relativePath) => Path.Join(Constants.AppDataFolderPath, relativePath);

  private static void WriteFile(string path, string contents = "test content") {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, contents);
  }

  private static void AddReadRule(string path, UserInformation user, AccessControlType type) {
    var fileSecurity = new FileInfo(path).GetAccessControl();
    fileSecurity.AddAccessRule(new FileSystemAccessRule(
        new SecurityIdentifier(user.Sid),
        FileSystemRights.Read,
        type
    ));
    new FileInfo(path).SetAccessControl(fileSecurity);
  }

  [Test]
  public async Task CanAccessPath_NullUserReturns401AndFalse() {
    var result = FileAccessInfo.CanAccessPath(@"C:\some\path", null, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(401);
  }

  [Test]
  public async Task CanAccessPath_DefaultIcoIsAlwaysAllowed() {
    var path = Constants.AppRoot + "default.ico";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_AssetsPngIsAlwaysAllowed() {
    var path = Constants.AssetsFolderPath + "/icon.png";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  // Replaces the file's permissions with read access for only the specified SIDs.
  private static void SetOnlyReadRules(string path, params string[] sids) {
    var fileSecurity = new FileInfo(path).GetAccessControl();
    fileSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
    foreach (FileSystemAccessRule rule in fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier))) {
      fileSecurity.RemoveAccessRule(rule);
    }

    // keep full control for the current user so that the test can delete the file afterward
    fileSecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, AccessControlType.Allow));
    foreach (var sid in sids) {
      fileSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid), FileSystemRights.Read, AccessControlType.Allow));
    }
    new FileInfo(path).SetAccessControl(fileSecurity);
  }

  private static void WriteManagedResource(string path, string sddl) {
    var resource = new Management.ManagedFileResource(path, null, "remoteapplicationname:s:test", null, null, null, null, new RawSecurityDescriptor(sddl));
    resource.WriteToFile();
  }

  /// <summary>
  /// For paths where the file's security descriptor is checked (such as the resources folder
  /// and the inject filestore), anonymous users can only read files that Everyone or Users can read.
  /// </summary>
  [Test]
  [Arguments(@"resources\app.rdp", "S-1-1-0")] // Everyone
  [Arguments(@"resources\app.rdp", "S-1-5-32-545")] // Users
  [Arguments(@"inject\filestore\file.js", "S-1-1-0")]
  [Arguments(@"inject\filestore\file.js", "S-1-5-32-545")]
  public async Task CanAccessPath_AnonymousUserAllowedWhenEveryoneOrUsersCanRead(string relativePath, string groupSid) {
    var path = AppDataPath(relativePath);
    WriteFile(path);
    SetOnlyReadRules(path, groupSid);

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  [Arguments(@"resources\app.rdp", "S-1-5-21-1000")] // a specific user
  [Arguments(@"resources\app.rdp", "S-1-5-11")] // Authenticated Users
  [Arguments(@"inject\filestore\file.js", "S-1-5-21-1000")]
  [Arguments(@"inject\filestore\file.js", "S-1-5-11")]
  public async Task CanAccessPath_AnonymousUserNotAllowedWhenOnlyOthersCanRead(string relativePath, string sid) {
    var path = AppDataPath(relativePath);
    WriteFile(path);
    SetOnlyReadRules(path, sid);

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserGets404ForMissingNonSpecialPath() {
    var path = AppDataPath(@"resources\nonexistent.rdp");

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  [Arguments(@"multiuser-resources\user\anonymous\app.rdp")]
  [Arguments(@"multiuser-resources\group\Everyone\app.rdp")]
  [Arguments(@"multiuser-resources\group\Users\app.rdp")]
  [Arguments(@"multiuser-resources\group\S-1-1-0\app.rdp")]
  [Arguments(@"multiuser-resources\group\S-1-5-32-545\app.rdp")]
  public async Task CanAccessPath_AnonymousUserAllowedForOwnMultiuserFolders(string relativePath) {
    var path = AppDataPath(relativePath);
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  [Arguments(@"multiuser-resources\group\Authenticated Users\app.rdp")]
  [Arguments(@"multiuser-resources\group\S-1-5-11\app.rdp")]
  public async Task CanAccessPath_AnonymousUserNotAllowedForAuthenticatedUsersMultiuserFolder(string relativePath) {
    var path = AppDataPath(relativePath);
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  [Arguments("S-1-1-0")] // Everyone
  [Arguments("S-1-5-32-545")] // Users
  public async Task CanAccessPath_AnonymousUserAllowedForManagedResourceGrantedToEveryoneOrUsers(string groupSid) {
    var path = AppDataPath(@"managed-resources\test.resource");
    WriteManagedResource(path, $"O:BAG:BAD:(A;;FA;;;{groupSid})");

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserNotAllowedForManagedResourceGrantedToAuthenticatedUsers() {
    var path = AppDataPath(@"managed-resources\test.resource");
    WriteManagedResource(path, "O:BAG:BAD:(A;;FA;;;S-1-5-11)");

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserNotAllowedForMultiuserPath() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\file.rdp");

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserAllowedForManagedResourceFileWithNoSecurityDescriptor() {
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(path, null, "remoteapplicationname:s:test", null, null, null, null);
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserNotAllowedForManagedResourceFileWithSecurityDescriptorWithoutAnonymousUser() {
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(
        path,
        null,
        "remoteapplicationname:s:test",
        null,
        null,
        null,
        null,
        new RawSecurityDescriptor("O:BAG:BAD:(A;;FA;;;S-1-5-21-1000)")
    );
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserAllowedForManagedResourceFileWithSecurityDescriptorWithAnonymousUser() {
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(
        path,
        null,
        "remoteapplicationname:s:test",
        null,
        null,
        null,
        null,
        new RawSecurityDescriptor($"O:BAG:BAD:(A;;FA;;;{UserInformation.AnonymousUser.Sid})")
    );
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_SlashesAreNormalized() {
    var path = (Constants.AppRoot + "default.ico").Replace('\\', '/');
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);

    path = Constants.AppRoot + "default.ico";

    result = FileAccessInfo.CanAccessPath(path, user, out status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessDifferentUserResourceFromMultiuserResources() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\file.rdp");
    var user = MakeUser(username: "bob");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessServedImage_AllowsEmbeddedImages() {
    var result = FileAccessInfo.CanAccessServedImage("resource://static/lib/assets/default.ico", MakeUser(), out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  /// <summary>
  /// A fallback image can point at another user's file, so the served file must be checked
  /// even when the requested managed resource was allowed.
  /// </summary>
  [Test]
  public async Task CanAccessServedImage_RejectsAnotherUsersFallbackImage() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\icon.png");
    WriteFile(path);

    var result = FileAccessInfo.CanAccessServedImage(path, MakeUser(username: "bob"), out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessServedImage_AllowsOwnImage() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\icon.png");
    WriteFile(path);

    var result = FileAccessInfo.CanAccessServedImage(path, MakeUser(username: "jdoe"), out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_CanAccessOwnUserResourceFromMultiuserResources() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\file.rdp");
    var user = MakeUser(username: "jdoe");
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_TraversalOutOfOwnMultiuserFolderUsesSecurityDescriptor() {
    var secretPath = AppDataPath("secret.config");
    var traversalPath = AppDataPath(@"multiuser-resources\user\jdoe\..\..\..\secret.config");
    var user = MakeUser(username: "jdoe");
    Directory.CreateDirectory(AppDataPath(@"multiuser-resources\user\jdoe"));
    WriteFile(secretPath, "secret");
    AddReadRule(secretPath, user, AccessControlType.Deny);

    var result = FileAccessInfo.CanAccessPath(traversalPath, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_TraversalIntoOtherUserMultiuserFolderIsDenied() {
    var path = AppDataPath(@"multiuser-resources\user\bob\..\jdoe\file.rdp");
    var user = MakeUser(username: "bob");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  /// <summary>
  /// Paths that look like multiuser resource paths but are outside of the App_Data folder
  /// must not receive the lenient folder-name permission check.
  /// </summary>
  [Test]
  public async Task CanAccessPath_MultiuserLookalikeOutsideAppDataUsesSecurityDescriptor() {
    var path = Path.Join(_tempTestRootPath, @"elsewhere\multiuser-resources\user\jdoe\file.rdp");
    var user = MakeUser(username: "jdoe");
    WriteFile(path);
    AddReadRule(path, user, AccessControlType.Deny);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_UserFolderPrefixDoesNotMatchUserFolder() {
    // "userjdoe" must not be treated as the "user" folder with username "jdoe"
    var path = AppDataPath(@"multiuser-resources\userjdoe\file.rdp");
    var user = MakeUser(username: "jdoe");
    WriteFile(path);
    AddReadRule(path, user, AccessControlType.Deny);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_FileDirectlyInsideMultiuserUserFolderIsDenied() {
    var path = AppDataPath(@"multiuser-resources\user\file.rdp");
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_ManagedResourcesFolderInsideOtherUserFolderUsesUsernameCheck() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(path, null, "remoteapplicationname:s:test", null, null, null, null);
    resource.WriteToFile();
    var user = MakeUser(username: "bob");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessOwnNonexistentResourceFromMultiuserResources() {
    var path = AppDataPath(@"multiuser-resources\user\jdoe\nonexistent.rdp");
    var user = MakeUser(username: "jdoe");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessNonMemberGroupResourceFromMultiuserResources() {
    var path = AppDataPath(@"multiuser-resources\group\admins\file.rdp");
    var user = MakeUser(groups: [new GroupInformation("users", "S-1-5-32-545")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessMemberGroupNonexistentResourceFromMultiuserResources_ByName() {
    var path = AppDataPath(@"multiuser-resources\group\admins\nonexistent.rdp");
    var user = MakeUser(groups: [new GroupInformation("admins", "S-1-5-32-544")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessMemberGroupNonexistentResourceFromMultiuserResources_BySid() {
    var path = AppDataPath(@"multiuser-resources\group\S-1-5-32-544\nonexistent.rdp");
    var user = MakeUser(groups: [new GroupInformation("Administrators", "S-1-5-32-544")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CanAccessMemberGroupResourceFromMultiuserResources_ByName() {
    var path = AppDataPath(@"multiuser-resources\group\admins\file.rdp");
    var user = MakeUser(groups: [new GroupInformation("admins", "S-1-5-32-544")]);
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_CanAccessMemberGroupResourceFromMultiuserResources_BySid() {
    var path = AppDataPath(@"multiuser-resources\group\S-1-5-32-544\file.rdp");
    var user = MakeUser(groups: [new GroupInformation("Administrators", "S-1-5-32-544")]);
    WriteFile(path);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_NonExistentManagedFileResourceReturnsFalseAnd404() {
    var path = AppDataPath(@"managed-resources\nonexistent.resource");
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsFalseAnd403WhenUserHasNoAccess() {
    var user = MakeUser();
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(
        path,
        null,
        "remoteapplicationname:s:test",
        null,
        null,
        null,
        null,
        new RawSecurityDescriptor($"O:BAG:BAD:(A;;FA;;;S-1-5-21-2)")
    );
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsTrueAnd200WhenUserHasAccess() {
    var user = MakeUser();
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(
        path,
        null,
        "remoteapplicationname:s:test",
        null,
        null,
        null,
        null,
        new RawSecurityDescriptor($"O:BAG:BAD:(A;;FA;;;{user.Sid})")
    );
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsFalseAnd403WhenUserHasNoAccessAndAnonymousUserHasAccess() {
    var user = MakeUser();
    var path = AppDataPath(@"managed-resources\test.resource");
    var resource = new Management.ManagedFileResource(
        path,
        null,
        "remoteapplicationname:s:test",
        null,
        null,
        null,
        null,
        new RawSecurityDescriptor($"O:BAG:BAD:(A;;FA;;;{UserInformation.AnonymousUser.Sid})")
    );
    resource.WriteToFile();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathNonExistentFileReturns404AndFalse() {
    var path = AppDataPath(@"resources\nonexistent.rdp");
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathFileReturns200AndTrue() {
    var user = MakeUser();
    var path = AppDataPath(@"resources\test.rdp");
    WriteFile(path);
    AddReadRule(path, user, AccessControlType.Allow);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathFileReturns403AndFalseWhenUserHasNoAccess() {
    var user = MakeUser();
    var path = AppDataPath(@"resources\test.rdp");
    WriteFile(path);
    AddReadRule(path, user, AccessControlType.Deny);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_OverloadWithoutOutParam_ReturnsCorrectResult() {
    var path = Constants.AppRoot + "default.ico";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user);

    await Assert.That(result).IsTrue();
  }
}
