using Microsoft.Win32;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities.Tests;

public class RegistryReaderTests {
  // HKLM\SOFTWARE is always present and has no SecurityDescriptor value.
  private static RegistryKey SoftwareKey() {
    var key = Registry.LocalMachine.OpenSubKey("SOFTWARE");
    if (key is null) {
      throw new InvalidOperationException("HKLM\\SOFTWARE key not found");
    }
    return key;
  }

  [Test]
  public async Task CanAccessRemoteApp_NullUser_Returns401AndFalse() {
    using var key = SoftwareKey();
    var result = RegistryReader.CanAccessRemoteApp(key, null!, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(401);
  }

  [Test]
  public async Task CanAccessRemoteApp_AnonymousUser_ReturnsTrue() {
    using var key = SoftwareKey();
    var result = RegistryReader.CanAccessRemoteApp(key, UserInformation.AnonymousUser, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessRemoteApp_UserWithNoGroupMemberships_Returns403AndFalse() {
    using var key = SoftwareKey();
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, []);

    var result = RegistryReader.CanAccessRemoteApp(key, user, out var status);

    await Assert.That(result).IsFalse();
    await Assert.That(status).IsEqualTo(403);
  }

  [Test]
  public async Task CanAccessRemoteApp_RemoteDesktopUser_WithNoSecurityDescriptor_ReturnsTrue() {
    using var key = SoftwareKey();
    var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    var result = RegistryReader.CanAccessRemoteApp(key, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessRemoteApp_LocalAdministrator_WithNoSecurityDescriptor_ReturnsTrue() {
    using var key = SoftwareKey();
    var groups = new GroupInformation[] { new("Administrators", "S-1-5-32-544") };
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    var result = RegistryReader.CanAccessRemoteApp(key, user, out var status);

    await Assert.That(result).IsTrue();
    await Assert.That(status).IsEqualTo(200);
  }

  [Test]
  public async Task CanAccessRemoteApp_OverloadWithoutHttpStatus_AnonymousUser_ReturnsTrue() {
    using var key = SoftwareKey();

    var result = RegistryReader.CanAccessRemoteApp(key, UserInformation.AnonymousUser);

    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task CanAccessRemoteApp_OverloadWithoutHttpStatus_UserWithNoGroups_ReturnsFalse() {
    using var key = SoftwareKey();
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, []);

    var result = RegistryReader.CanAccessRemoteApp(key, user);

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task CanAccessRemoteApp_SecurityDescriptorAllowsRDUsersGroup_ReturnsTrue() {
    var keyPath = "SOFTWARE\\RAWebTest_" + Guid.NewGuid().ToString("N");
    try {
      var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
      var user = new UserInformation("S-1-5-21-1111111111-2222222222-3333333333-1001", "jdoe", "DOMAIN", null, groups);
      using var tempKey = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
      // only allow Remote Desktop Users group
      tempKey.SetValue("SecurityDescriptor", "D:(A;;0x1;;;S-1-5-32-555)");

      var result = RegistryReader.CanAccessRemoteApp(tempKey, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      Registry.CurrentUser.DeleteSubKey(keyPath, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task CanAccessRemoteApp_SecurityDescriptorDoesNotIncludeRDUsersGroups_Returns403AndFalse() {
    var keyPath = "SOFTWARE\\RAWebTest_" + Guid.NewGuid().ToString("N");
    try {
      var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
      var user = new UserInformation("S-1-5-21-1111111111-2222222222-3333333333-1001", "jdoe", "DOMAIN", null, groups);
      using var tempKey = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
      // only allow Administrators group (the test user is not an admin)
      tempKey.SetValue("SecurityDescriptor", "D:(A;;0x1;;;S-1-5-32-544)");

      var result = RegistryReader.CanAccessRemoteApp(tempKey, user, out var status);

      await Assert.That(result).IsFalse();
      await Assert.That(status).IsEqualTo(403);
    }
    finally {
      Registry.CurrentUser.DeleteSubKey(keyPath, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task CanAccessRemoteApp_SecurityDescriptorAllowsUserBySid_ReturnsTrue() {
    var keyPath = "SOFTWARE\\RAWebTest_" + Guid.NewGuid().ToString("N");
    try {
      var userSid = "S-1-5-21-1111111111-2222222222-3333333333-1001";
      var groups = new GroupInformation[] { new("Remote Desktop Users", "S-1-5-32-555") };
      var user = new UserInformation(userSid, "jdoe", "DOMAIN", null, groups);
      using var tempKey = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
      // only allow this specific user
      tempKey.SetValue("SecurityDescriptor", $"D:(A;;0x1;;;{userSid})");

      var result = RegistryReader.CanAccessRemoteApp(tempKey, user, out var status);

      await Assert.That(result).IsTrue();
      await Assert.That(status).IsEqualTo(200);
    }
    finally {
      Registry.CurrentUser.DeleteSubKey(keyPath, throwOnMissingSubKey: false);
    }
  }


}

// NotInParallel because these tests mutate Constants.AppRoot and PoliciesManager.s_appSettingsPath.
[NotInParallel]
public class RegistryReaderRdpTests {
  private string _tempAppRoot = "";
  private string _tempConfigPath = "";

  [Before(Test)]
  public void Setup() {
    _tempAppRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempAppRoot);
    Constants.AppRoot = _tempAppRoot;
    AppId.Initialize();

    _tempConfigPath = Path.GetTempFileName();
    File.WriteAllText(_tempConfigPath, "<configuration><appSettings></appSettings></configuration>");
    _ = new PoliciesManager(_tempConfigPath);
  }

  [After(Test)]
  public void Cleanup() {
    if (File.Exists(_tempConfigPath)) {
      File.Delete(_tempConfigPath);
    }
    if (Directory.Exists(_tempAppRoot)) {
      Directory.Delete(_tempAppRoot, recursive: true);
    }
  }

  private static readonly string s_tsAppsPath =
      @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";

  private static string CreateTsAppEntry(string appName) {
    using var appsKey = Registry.LocalMachine.CreateSubKey(s_tsAppsPath, writable: true);
    using var appKey = appsKey?.CreateSubKey(appName);
    appKey?.SetValue("Name", "Test Notepad");
    appKey?.SetValue("Path", @"C:\Windows\System32\notepad.exe");
    appKey?.SetValue("VPath", @"C:\Windows\System32\notepad.exe");
    appKey?.SetValue("CommandLineSetting", 1);
    appKey?.SetValue("IconIndex", 0);
    return appName;
  }

  private static void DeleteTsAppEntry(string appName) {
    using var appsKey = Registry.LocalMachine.OpenSubKey(s_tsAppsPath, writable: true);
    appsKey?.DeleteSubKeyTree(appName, throwOnMissingSubKey: false);
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingDisabled_DesktopThrowsInvalidOperationException() {
    // RegistryApps.Enabled = "true" means the legacy TSAppAllowList path is active,
    // which does not support centralized desktop publishing.
    PoliciesManager.Set("RegistryApps.Enabled", "true");

    await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => {
      RegistryReader.ConstructRdpFileFromRegistry("any", isDesktop: true);
    }));
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingEnabled_DesktopReturnsRdpFileWithFullAddressAndDesktopMode() {
    await Assert.That(ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var desktopsPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\RemoteDesktops";
    try {
      using (var desktopsKey = Registry.LocalMachine.CreateSubKey(desktopsPath, writable: true)) {
        using var desktopKey = desktopsKey?.CreateSubKey(collectionName);
        desktopKey?.SetValue("Name", "Test Desktop");
        desktopKey?.SetValue("ShowInPortal", 1);
      }

      var rdp = RegistryReader.ConstructRdpFileFromRegistry("ignored", isDesktop: true);

      await Assert.That(rdp).IsNotNull();
      await Assert.That(rdp.Contains("full address:s:")).IsTrue();
      // desktop sessions use remoteapplicationmode:i:0 (not a RemoteApp)
      await Assert.That(rdp.Contains("remoteapplicationmode:i:0")).IsTrue();
    }
    finally {
      using var desktopsKey = Registry.LocalMachine.OpenSubKey(desktopsPath, writable: true);
      desktopsKey?.DeleteSubKeyTree(collectionName, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingDisabled_NonExistentRemoteAppThrowsException() {
    PoliciesManager.Set("RegistryApps.Enabled", "true"); // disable centralized publishing

    await Assert.ThrowsAsync<Exception>(() => Task.Run(() => {
      return RegistryReader.ConstructRdpFileFromRegistry("missing_app");
    }));
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingDisabled_ReturnsAppRdpFileWithExpectedProperties() {
    await Assert.That(ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    PoliciesManager.Set("RegistryApps.Enabled", "true"); // disable centralized publishing

    var appName = "RAWebTestApp_" + Guid.NewGuid().ToString("N")[..8];
    try {
      CreateTsAppEntry(appName);

      var rdp = RegistryReader.ConstructRdpFileFromRegistry(appName);

      await Assert.That(rdp).IsNotNull();
      await Assert.That(rdp.Contains("full address:s:")).IsTrue();
      await Assert.That(rdp.Contains("remoteapplicationname:s:Test Notepad")).IsTrue();
      await Assert.That(rdp.Contains(@"remoteapplicationprogram:s:C:\Windows\System32\notepad.exe")).IsTrue();
      await Assert.That(rdp.Contains("remoteapplicationmode:i:1")).IsTrue();
    }
    finally {
      DeleteTsAppEntry(appName);
    }
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingEnabled_NonExistentRemoteAppThrowsException() {
    await Assert.ThrowsAsync<Exception>(() => Task.Run(() => {
      RegistryReader.ConstructRdpFileFromRegistry("missing_app");
    }));
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_CentralizedPublishingEnabled_ReturnsAppRdpFileWithExpectedProperties() {
    await Assert.That(ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var appName = "RAWebTestApp_" + Guid.NewGuid().ToString("N")[..8];
    var appsPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\Applications";
    try {
      using (var appsKey = Registry.LocalMachine.CreateSubKey(appsPath, writable: true)) {
        using var appKey = appsKey?.CreateSubKey(appName);
        appKey?.SetValue("Name", "Test Notepad");
        appKey?.SetValue("Path", @"C:\Windows\System32\notepad.exe");
        appKey?.SetValue("VPath", @"C:\Windows\System32\notepad.exe");
        appKey?.SetValue("CommandLineSetting", 1);
        appKey?.SetValue("IconIndex", 0);
        appKey?.SetValue("ShowInPortal", 1);
      }

      var rdp = RegistryReader.ConstructRdpFileFromRegistry(appName);

      await Assert.That(rdp).IsNotNull();
      await Assert.That(rdp.Contains("full address:s:")).IsTrue();
      await Assert.That(rdp.Contains("remoteapplicationname:s:Test Notepad")).IsTrue();
      await Assert.That(rdp.Contains(@"remoteapplicationprogram:s:C:\Windows\System32\notepad.exe")).IsTrue();
      await Assert.That(rdp.Contains("remoteapplicationmode:i:1")).IsTrue();
    }
    finally {
      using var appsKey = Registry.LocalMachine.OpenSubKey(appsPath, writable: true);
      appsKey?.DeleteSubKeyTree(appName, throwOnMissingSubKey: false);
    }
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_AdditionalProperties_SemicolonSeparatesIntoMultipleLines() {
    await Assert.That(ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");
    PoliciesManager.Set("RegistryApps.Enabled", "true");
    // Each ; in the policy value becomes a newline, producing separate RDP lines.
    PoliciesManager.Set("RegistryApps.AdditionalProperties", "audiomode:i:2;compression:i:1");

    var appName = "RAWebTestApp_" + Guid.NewGuid().ToString("N")[..8];
    try {
      CreateTsAppEntry(appName);

      var rdp = RegistryReader.ConstructRdpFileFromRegistry(appName);

      await Assert.That(rdp.Contains("audiomode:i:2")).IsTrue();
      await Assert.That(rdp.Contains("compression:i:1")).IsTrue();
    }
    finally {
      DeleteTsAppEntry(appName);
    }
  }

  [Test]
  public async Task ConstructRdpFileFromRegistry_AdditionalProperties_EscapedSemicolonPreservedInValue() {
    await Assert.That(ElevatedPrivileges.Check()).IsTrue().Because("test requires HKLM write access");

    PoliciesManager.Set("RegistryApps.Enabled", "true");
    // The value contains an escaped semicolon (\;), which should be preserved in the RDP file.
    PoliciesManager.Set("RegistryApps.AdditionalProperties", @"testprop:s:a\;b");

    var appName = "RAWebTestApp_" + Guid.NewGuid().ToString("N")[..8];
    try {
      CreateTsAppEntry(appName);

      var rdp = RegistryReader.ConstructRdpFileFromRegistry(appName);

      // the escaped semicolon should be preserved in the RDP file so that the value is "a;b"
      await Assert.That(rdp.Contains("testprop:s:a;b")).IsTrue();

      // "b" must not appear as its own standalone RDP property
      await Assert.That(rdp.Contains("\r\nb\r\n") || rdp.StartsWith("b\r\n")).IsFalse();
    }
    finally {
      DeleteTsAppEntry(appName);
    }
  }
}
