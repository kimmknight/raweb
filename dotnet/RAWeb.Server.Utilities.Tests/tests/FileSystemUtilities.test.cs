using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Utilities.Tests;

public class FileAccessInfoTests {
  private static UserInformation MakeUser(
      string sid = "S-1-5-21-1000",
      string username = "jdoe",
      string domain = "DOMAIN",
      GroupInformation[]? groups = null
  ) => new(sid, username, domain, null, groups ?? []);

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

  /// <summary>
  /// For paths where the file's security descriptor is checked, anonymous users
  /// are always allowed read access. This does not apply to special paths
  /// such as multiuser-resources and other managed resources.
  /// </summary>
  /// <returns></returns>
  [Test]
  public async Task CanAccessPath_AnonymousUserAllowedForNonSpecialPath() {
    var path = @"C:\nonexistent\resource.rdp";

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserNotAllowedForMultiuserPath() {
    var path = @"C:\raweb\multiuser-resources\user\jdoe\file.rdp";

    var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserAllowedForManagedResourceFileWithNoSecurityDescriptor() {
    var tempDir = Path.Combine(Path.GetTempPath(), "RAWebTest");

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "test.resource");
      var resource = new Management.ManagedFileResource(path, null, "remoteapplicationname:s:test", null, null, null, null);
      resource.WriteToFile();

      var result = FileAccessInfo.CanAccessPath(path, UserInformation.AnonymousUser, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserNotAllowedForManagedResourceFileWithSecurityDescriptorWithoutAnonymousUser() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "managed-resources/test.resource");
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
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_AnonymousUserAllowedForManagedResourceFileWithSecurityDescriptorWithAnonymousUser() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "managed-resources/test.resource");
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
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
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
    var path = @"C:\raweb\multiuser-resources\user\jdoe\file.rdp";
    var user = MakeUser(username: "bob");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_CanAccessOwnUserResourceFromMultiuserResources() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var path = Path.Join(tempDir, @"App_Data\multiuser-resources\user\jdoe\file.rdp");
    var user = MakeUser(username: "jdoe");

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "test content");

      var result = FileAccessInfo.CanAccessPath(path, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }

  }

  [Test]
  public async Task CanAccessPath_CannotAccessOwnNonexistentResourceFromMultiuserResources() {
    var path = @"C:\raweb\multiuser-resources\user\jdoe\nonexistent.rdp";
    var user = MakeUser(username: "jdoe");

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessNonMemberGroupResourceFromMultiuserResources() {
    var path = @"C:\raweb\multiuser-resources\group\admins\file.rdp";
    var user = MakeUser(groups: [new GroupInformation("users", "S-1-5-32-545")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessMemberGroupNonexistentResourceFromMultiuserResources_ByName() {
    var path = @"C:\raweb\multiuser-resources\group\admins\nonexistent.rdp";
    var user = MakeUser(groups: [new GroupInformation("admins", "S-1-5-32-544")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CannotAccessMemberGroupNonexistentResourceFromMultiuserResources_BySid() {
    var path = @"C:\raweb\multiuser-resources\group\S-1-5-32-544\nonexistent.rdp";
    var user = MakeUser(groups: [new GroupInformation("Administrators", "S-1-5-32-544")]);

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_CanAccessMemberGroupResourceFromMultiuserResources_ByName() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var path = Path.Join(tempDir, @"App_Data\multiuser-resources\group\admins\file.rdp");
    var user = MakeUser(groups: [new GroupInformation("admins", "S-1-5-32-544")]);

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "test content");

      var result = FileAccessInfo.CanAccessPath(path, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_CanAccessMemberGroupResourceFromMultiuserResources_BySid() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var path = Path.Join(tempDir, @"App_Data\multiuser-resources\group\S-1-5-32-544\file.rdp");
    var user = MakeUser(groups: [new GroupInformation("Administrators", "S-1-5-32-544")]);

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "test content");

      var result = FileAccessInfo.CanAccessPath(path, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_NonExistentManagedFileResourceReturnsFalseAnd404() {
    var path = @"C:\raweb\managed-resources\nonexistent.resource";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsFalseAnd403WhenUserHasNoAccess() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var user = MakeUser();

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "managed-resources/test.resource");
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
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsTrueAnd200WhenUserHasAccess() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var user = MakeUser();

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "managed-resources/test.resource");
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
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_ManagedResourceFileReturnsFalseAnd403WhenUserHasNoAccessAndAnonymousUserHasAccess() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var user = MakeUser();

    try {
      Directory.CreateDirectory(tempDir);

      var path = Path.Combine(tempDir, "managed-resources/test.resource");
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
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathNonExistentFileReturns404AndFalse() {
    var path = @"C:\raweb\resources\nonexistent.rdp";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(404);
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathFileReturns200AndTrue() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var user = MakeUser();
    var path = Path.Combine(tempDir, "App_Data/resources/test.rdp");

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "test content");

      // grant access to the file for the user
      var fileSecurity = new FileInfo(path).GetAccessControl();
      fileSecurity.AddAccessRule(new FileSystemAccessRule(
          new SecurityIdentifier(user.Sid),
          FileSystemRights.Read,
          AccessControlType.Allow
      ));
      new FileInfo(path).SetAccessControl(fileSecurity);

      var result = FileAccessInfo.CanAccessPath(path, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_NonSpecialPathFileReturns403AndFalseWhenUserHasNoAccess() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var user = MakeUser();
    var path = Path.Combine(tempDir, "App_Data/resources/test.rdp");

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "test content");

      // deny access to the file for the user
      var fileSecurity = new FileInfo(path).GetAccessControl();
      fileSecurity.AddAccessRule(new FileSystemAccessRule(
          new SecurityIdentifier(user.Sid),
          FileSystemRights.Read,
          AccessControlType.Deny
      ));
      new FileInfo(path).SetAccessControl(fileSecurity);

      var result = FileAccessInfo.CanAccessPath(path, user, out var status);

      await Assert.That(result).IsFalse();
      await Assert.That(status).IsEqualTo(403);
    }
    finally {
      // Clean up the temporary file and directory
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task CanAccessPath_OverloadWithoutOutParam_ReturnsCorrectResult() {
    var path = Constants.AppRoot + "default.ico";
    var user = MakeUser();

    var result = FileAccessInfo.CanAccessPath(path, user);

    await Assert.That(result).IsTrue();
  }
}
