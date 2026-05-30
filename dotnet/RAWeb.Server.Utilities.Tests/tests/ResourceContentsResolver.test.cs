using System.Net;
using Microsoft.Win32;

namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class ResourceContentsResolverTests {
  private static UserInformation MakeUser() => new("S-1-5-21-1000", "jdoe", "DOMAIN");

  private static ResourceContentsResolver.FailedResourceResult AssertFailed(ResourceContentsResolver.ResourceResult result) {
    if (result is not ResourceContentsResolver.FailedResourceResult failed) {
      throw new InvalidCastException($"Expected FailedResourceResult but got {result.GetType().Name}.");
    }
    return failed;
  }

  private string _tempTestRootPath = null!;

  [Before(Test)]
  public void SetUpTempAppData() {
    // set temporary AppRoot location
    _tempTestRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempTestRootPath);
    Constants.AppRoot = _tempTestRootPath;
    AppId.Initialize();
    Console.WriteLine($"Using AppRoot: {Constants.AppRoot}");

    // add test resources
    var resourcesPath = Path.Combine(Constants.AppDataFolderPath, "resources");
    Directory.CreateDirectory(resourcesPath);
    var multiuserResourcesPath = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources");
    Directory.CreateDirectory(multiuserResourcesPath);
    var managedResourcesPath = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    Directory.CreateDirectory(managedResourcesPath);

    var testRdpFilePath = Path.Combine(resourcesPath, "testfile.rdp");
    File.WriteAllText(testRdpFilePath, "dummy content");
    Console.WriteLine($"Created file: {testRdpFilePath}");

    var testMultiuserUserRdpFilePath = Path.Combine(multiuserResourcesPath, "user", "jdoe", "testfile.rdp");
    Directory.CreateDirectory(Path.GetDirectoryName(testMultiuserUserRdpFilePath)!);
    File.WriteAllText(testMultiuserUserRdpFilePath, "dummy content");
    Console.WriteLine($"Created file: {testMultiuserUserRdpFilePath}");

    var testMultiuserGroupRdpFilePath = Path.Combine(multiuserResourcesPath, "group", "Administrators", "testfile.rdp");
    Directory.CreateDirectory(Path.GetDirectoryName(testMultiuserGroupRdpFilePath)!);
    File.WriteAllText(testMultiuserGroupRdpFilePath, "dummy content");
    Console.WriteLine($"Created file: {testMultiuserGroupRdpFilePath}");

    var testMultiuserGroupSidRdpFilePath = Path.Combine(multiuserResourcesPath, "group", "S-1-5-32-544", "testfile.rdp");
    Directory.CreateDirectory(Path.GetDirectoryName(testMultiuserGroupSidRdpFilePath)!);
    File.WriteAllText(testMultiuserGroupSidRdpFilePath, "dummy content");
    Console.WriteLine($"Created file: {testMultiuserGroupSidRdpFilePath}");
  }

  [After(Test)]
  public void CleanUpTempAppData() {
    if (Directory.Exists(_tempTestRootPath)) {
      Directory.Delete(_tempTestRootPath, true);
      Console.WriteLine($"Deleted temporary AppRoot: {Constants.AppRoot}");
    }
  }

  [Test]
  public async Task ResolveResource_Rdp_PathOutsideAllowedRootReturnsForbidden() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"notallowed\file.rdp", ResourceOrigin.Rdp);
    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);

    var result2 = ResourceContentsResolver.ResolveResource(MakeUser(), @"C:\Windows\file.rdp", ResourceOrigin.Rdp);
    var failed2 = AssertFailed(result2);
    await Assert.That(failed2.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);

    var result3 = ResourceContentsResolver.ResolveResource(MakeUser(), @"..\..\file.rdp", ResourceOrigin.Rdp);
    var failed3 = AssertFailed(result3);
    await Assert.That(failed3.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);
  }

  [Test]
  public async Task ResolveResource_Rdp_MultiuserPathMissingTypeReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"multiuser-resources\file.rdp", ResourceOrigin.Rdp);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_Rdp_MultiuserPathMissingGroupOrUserNameReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"multiuser-resources\user\file.rdp", ResourceOrigin.Rdp);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_Rdp_NoFileAtValidPathReturnsNotFound() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"resources\nonexistent_file_xyz.rdp", ResourceOrigin.Rdp);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.NotFound);
  }

  [Test]
  public async Task ResolveResource_Rdp_AppDataPrefixIsStrippedBeforePathValidation() {
    // "App_Data/" prefix is stripped; the remainder resolves inside resources/
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"App_Data/resources/nonexistent_file_xyz.rdp", ResourceOrigin.Rdp);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.NotFound);
  }

  [Test]
  public async Task ResolveResource_Rdp_NoFileAtMultiuserPathWithValidTypeAndNameReturnsNotFound() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"multiuser-resources\user\jdoe\nonexistent_xyz.rdp", ResourceOrigin.Rdp);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.NotFound);
  }

  [Test]
  public async Task ResolveResource_Rdp_AppendsRdpFileExtensionIfMissing() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"resources\testfile", ResourceOrigin.Rdp);

    await Assert.That(result).IsNotNull();
    var isResourceResult = result is ResourceContentsResolver.ResourceResult;
    await Assert.That(isResourceResult).IsTrue();
  }

  [Test]
  public async Task ResolveResource_Rdp_FindsRdpFile() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"resources\testfile.rdp", ResourceOrigin.Rdp);

    await Assert.That(result).IsNotNull();
    var isResourceResult = result is ResourceContentsResolver.ResourceResult;
    await Assert.That(isResourceResult).IsTrue();
  }

  [Test]
  public async Task ResolveResource_RegistryDesktop_PathWithBackslashReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"folder\key", ResourceOrigin.RegistryDesktop);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_RegistryDesktop_PathWithForwardSlashReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), "folder/key", ResourceOrigin.RegistryDesktop);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_Registry_PathWithBackslashReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"folder\key", ResourceOrigin.Registry);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_Registry_PathWithForwardSlashReturnsBadRequest() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), "folder/key", ResourceOrigin.Registry);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.BadRequest);
  }

  [Test]
  public async Task ResolveResource_Registry_NonExistentAppThrowsArgumentException() {
    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => {
      ResourceContentsResolver.ResolveResource(MakeUser(), "nonexistent_app_xyz_12345", ResourceOrigin.Registry);
    }));
  }

  [Test]
  public async Task ResolveResource_Registry_ExistingAppReturnsResolvedResultForAnonymousUser() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var appName = "RAWebRCRTest_" + Guid.NewGuid().ToString("N")[..8];
    var appsPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\Applications";
    try {
      using (var appsKey = Registry.LocalMachine.CreateSubKey(appsPath, writable: true)) {
        using var appKey = appsKey?.CreateSubKey(appName);
        appKey?.SetValue("Name", "Test App");
        appKey?.SetValue("Path", @"C:\Windows\System32\notepad.exe");
        appKey?.SetValue("VPath", @"C:\Windows\System32\notepad.exe");
        appKey?.SetValue("CommandLineSetting", 1);
        appKey?.SetValue("IconIndex", 0);
      }

      var result = ResourceContentsResolver.ResolveResource(UserInformation.AnonymousUser, appName, ResourceOrigin.Registry);

      await Assert.That(result is ResourceContentsResolver.ResolvedResourceResult).IsTrue();
      var resolved = (ResourceContentsResolver.ResolvedResourceResult)result;
      await Assert.That(resolved.RdpFileContents.Contains("full address:s:")).IsTrue();
      await Assert.That(resolved.FileName).IsEqualTo(appName + ".rdp");
    }
    finally {
      using var appsKey = Registry.LocalMachine.OpenSubKey(appsPath, writable: true);
      appsKey?.DeleteSubKeyTree(appName, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task ResolveResource_RegistryDesktop_NonExistentDesktopReturnsNotFound() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), "nonexistent_desktop_xyz_12345", ResourceOrigin.RegistryDesktop);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.NotFound);
  }

  [Test]
  public async Task ResolveResource_RegistryDesktop_ExistingDesktopReturnsResolvedResultForAnonymousUser() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var desktopsPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\RemoteDesktops";
    try {
      using (var desktopsKey = Registry.LocalMachine.CreateSubKey(desktopsPath, writable: true)) {
        using var desktopKey = desktopsKey?.CreateSubKey(collectionName);
        desktopKey?.SetValue("Name", "Test Desktop");
        desktopKey?.SetValue("ShowInPortal", 1);
      }

      var result = ResourceContentsResolver.ResolveResource(UserInformation.AnonymousUser, collectionName, ResourceOrigin.RegistryDesktop);

      await Assert.That(result is ResourceContentsResolver.ResolvedResourceResult).IsTrue();
      var resolved = (ResourceContentsResolver.ResolvedResourceResult)result;
      await Assert.That(resolved.RdpFileContents.Contains("full address:s:")).IsTrue();
      await Assert.That(resolved.RdpFileContents.Contains("remoteapplicationmode:i:0")).IsTrue();
      await Assert.That(resolved.FileName).IsEqualTo(collectionName + ".rdp");
    }
    finally {
      using var desktopsKey = Registry.LocalMachine.OpenSubKey(desktopsPath, writable: true);
      desktopsKey?.DeleteSubKeyTree(collectionName, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task ResolveResource_ManagedResource_PathOutsideRootReturnsForbidden() {
    var result = ResourceContentsResolver.ResolveResource(MakeUser(), @"..\..\..\etc\passwd", ResourceOrigin.ManagedResource);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);
  }

  [Test]
  public async Task ResolveResource_ManagedResource_NonExistentFileThrowsFileNotFoundException() {
    await Assert.ThrowsAsync<FileNotFoundException>(() => Task.Run(() => {
      ResourceContentsResolver.ResolveResource(MakeUser(), @"managed-resources\nonexistent_xyz_12345", ResourceOrigin.ManagedResource);
    }));
  }

  private static string WriteManagedResource(string fileName, string rdpContent, System.Security.AccessControl.RawSecurityDescriptor? securityDescriptor = null) {
    var managedResourcesPath = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    var filePath = Path.Combine(managedResourcesPath, fileName + ".resource");
    var resource = new Management.ManagedFileResource(filePath, null, rdpContent, null, null, null, null, securityDescriptor);
    resource.WriteToFile();
    return $@"managed-resources\{fileName}.resource";
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithNoSecurityDescriptorReturnsResolvedResult() {
    var relativePath = WriteManagedResource("test-app", "full address:s:myserver\r\nremoteapplicationname:s:My App\r\n");

    var result = ResourceContentsResolver.ResolveResource(MakeUser(), relativePath, ResourceOrigin.ManagedResource);

    await Assert.That(result is ResourceContentsResolver.ResolvedResourceResult).IsTrue();
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileReturnsCorrectFileName() {
    var relativePath = WriteManagedResource("my-resource", "full address:s:myserver\r\n");

    var result = ResourceContentsResolver.ResolveResource(MakeUser(), relativePath, ResourceOrigin.ManagedResource);

    var resolved = (ResourceContentsResolver.ResolvedResourceResult)result;
    await Assert.That(resolved.FileName).IsEqualTo("my-resource.rdp");
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileReturnsRdpContents() {
    var userInfo = MakeUser();
    var relativePath = WriteManagedResource("test-rdp", "full address:s:myserver\r\nremoteapplicationname:s:My App\r\n");

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    var resolved = (ResourceContentsResolver.ResolvedResourceResult)result;
    await Assert.That(resolved.RdpFileContents.Contains("full address:s:myserver")).IsTrue();
    await Assert.That(resolved.RdpFileContents.Contains("remoteapplicationname:s:My App")).IsTrue();
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithSecurityDescriptorAllowingUserReturnsResolvedResult() {
    var userInfo = MakeUser();
    var sd = new System.Security.AccessControl.RawSecurityDescriptor($"D:(A;;0x1;;;{userInfo.Sid})");
    var relativePath = WriteManagedResource("allowed-resource", "full address:s:myserver\r\n", sd);

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    await Assert.That(result is ResourceContentsResolver.ResolvedResourceResult).IsTrue();
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithSecurityDescriptorWithoutUserReturnsForbidden() {
    var userInfo = MakeUser();
    var adminSid = "S-1-5-32-544"; // test user is not an admin, so this should result in forbidden
    var sd = new System.Security.AccessControl.RawSecurityDescriptor($"D:(A;;0x1;;;{adminSid})");
    var relativePath = WriteManagedResource("restricted-resource", "full address:s:myserver\r\n", sd);

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithSecurityDescriptorAllowingUsersGroupReturnsResolvedResult() {
    var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
    var userInfo = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);
    var sd = new System.Security.AccessControl.RawSecurityDescriptor("D:(A;;0x1;;;S-1-5-32-555)");
    var relativePath = WriteManagedResource("group-allowed-resource", "full address:s:myserver\r\n", sd);

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    await Assert.That(result is ResourceContentsResolver.ResolvedResourceResult).IsTrue();
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithSecurityDescriptorNotIncludingUsersGroupReturnsForbidden() {
    // User is in Remote Desktop Users (S-1-5-32-555) but the SD only allows Administrators (S-1-5-32-544).
    var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
    var userInfo = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);
    var sd = new System.Security.AccessControl.RawSecurityDescriptor("D:(A;;0x1;;;S-1-5-32-544)");
    var relativePath = WriteManagedResource("group-denied-resource", "full address:s:myserver\r\n", sd);

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);
  }

  [Test]
  public async Task ResolveResource_ManagedResource_ExistingFileWithSecurityDesktopAllowingAndDenyingUserReturnsForbidden() {
    var userInfo = MakeUser();
    var sd = new System.Security.AccessControl.RawSecurityDescriptor($"D:(A;;0x1;;;{userInfo.Sid})(D;;0x1;;;{userInfo.Sid})");
    var relativePath = WriteManagedResource("conflicting-resource", "full address:s:myserver\r\n", sd);

    var result = ResourceContentsResolver.ResolveResource(userInfo, relativePath, ResourceOrigin.ManagedResource);

    var failed = AssertFailed(result);
    await Assert.That(failed.PermissionHttpStatus).IsEqualTo(HttpStatusCode.Forbidden);
  }
}
