namespace RAWeb.Server.Utilities.Tests;

public class ResourceTests {

  private static Resource MakeResource(
      string title = "My App",
      string fullAddress = "myserver",
      string? appProgram = null,
      string alias = "alias",
      string? appFileExtCSV = "",
      DateTime? lastUpdated = null,
      string[]? virtualFolders = null,
      ResourceOrigin origin = ResourceOrigin.RegistryDesktop,
      string source = ""
  ) => new(title, fullAddress, appProgram, alias, appFileExtCSV, lastUpdated ?? DateTime.MinValue, virtualFolders!, origin, source);

  [Test]
  public async Task Constructor_ThrowsWhenFullAddressIsEmpty() {
    var action = () => MakeResource(fullAddress: "");

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_ThrowsWhenSourceIsEmptyAndOriginIsNotRegistryDesktop() {
    var action = () => MakeResource(source: "", origin: ResourceOrigin.Rdp);

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_ThrowsWhenSourceIsNullAndOriginIsNotRegistryDesktop() {
    var action = () => MakeResource(source: null!, origin: ResourceOrigin.Rdp);

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_RdpFileOrigin_ThrowsWhenSourceIsInvalidPath() {
    var action = () => MakeResource(source: "C:\\myappthatdoesnotexist.rdp", origin: ResourceOrigin.Rdp);

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_ManagedResourceFileOrigin_ThrowsWhenSourceIsInvalidPath() {
    var action = () => MakeResource(source: "C:\\myappthatdoesnotexist.resource", origin: ResourceOrigin.ManagedResource);

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_RegistryAppOrigin_ThrowsWhenSourceIsNotAppNameWithSubkeyInTSAppAllowList() {
    var action = () => MakeResource(source: "MyAppThatDoesNotExistInRegistryTSAppAllowList", origin: ResourceOrigin.Registry);

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(action));
  }

  [Test]
  public async Task Constructor_ThrowsWhenTitleIsEmpty() {
    var action = () => MakeResource(title: "");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task Constructor_ThrowsWhenTitleIsNull() {
    var action = () => MakeResource(title: null!);

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task Constructor_SetsLastUpdatedToNowWhenDateTimeIsMinValue() {
    var resource = MakeResource();

    await Assert.That(resource.LastUpdated).IsNotEqualTo(DateTime.MinValue);
  }

  [Test]
  public async Task Constructor_SetsLastUpdatedToProvidedValueWhenNotMinValue() {
    var selectedDateTime = new DateTime(2026, 6, 4, 12, 0, 0);
    var resource = MakeResource(lastUpdated: selectedDateTime);

    await Assert.That(resource.LastUpdated).IsEqualTo(selectedDateTime);
  }

  [Test]
  public async Task Constructor_SetsTypeToDesktopWhenAppProgramIsNull() {
    var resource = MakeResource(appProgram: null);

    await Assert.That(resource.Type).IsEqualTo(ResourceType.Desktop);
    await Assert.That(resource.IsDesktop).IsTrue();
    await Assert.That(resource.IsApp).IsFalse();
  }

  [Test]
  public async Task Constructor_SetsTypeToRemoteAppWhenAppProgramIsProvided() {
    var resource = MakeResource(appProgram: @"C:\myappthatdoesnotexist.exe");

    await Assert.That(resource.Type).IsEqualTo(ResourceType.RemoteApp);
    await Assert.That(resource.IsApp).IsTrue();
    await Assert.That(resource.IsDesktop).IsFalse();
  }

  [Test]
  public async Task Constructor_DefaultsVirtualFoldersToSlashWhenNull() {
    var resource = MakeResource(virtualFolders: null);

    await Assert.That(resource.VirtualFolders.Length).IsEqualTo(1);
    await Assert.That(resource.VirtualFolders[0]).IsEqualTo("/");
  }

  [Test]
  public async Task FileExtensions_ReturnsEmptyArrayWhenAppFileExtCsvIsEmpty() {
    var resource = MakeResource(appProgram: "myappthatdoesnotexist.exe", appFileExtCSV: "");

    await Assert.That(resource.FileExtensions!.Length).IsEqualTo(0);
  }

  [Test]
  public async Task FileExtensions_ReturnsEmptyArrayWhenAppFileExtCsvIsNull() {
    var resource = MakeResource(appProgram: "myappthatdoesnotexist.exe", appFileExtCSV: null);

    await Assert.That(resource.FileExtensions!.Length).IsEqualTo(0);
  }

  [Test]
  public async Task FileExtensions_SplitsCsvIntoParts() {
    var resource = MakeResource(appProgram: "myappthatdoesnotexist.exe", appFileExtCSV: ".txt,.xlsx,.pdf");

    await Assert.That(resource.FileExtensions!.Length).IsEqualTo(3);
    await Assert.That(resource.FileExtensions[0]).IsEqualTo(".txt");
    await Assert.That(resource.FileExtensions[1]).IsEqualTo(".xlsx");
    await Assert.That(resource.FileExtensions[2]).IsEqualTo(".pdf");
  }

  [Test]
  public async Task GetResourceGUID_ReturnsSameGuidForSameContent() {
    var content = "full address:s:myserver\r\nremoteapplicationname:s:MyApp";

    var guid1 = Resource.GetResourceGUID(content);
    var guid2 = Resource.GetResourceGUID(content);

    await Assert.That(guid1).IsEqualTo(guid2);
  }

  [Test]
  public async Task GetResourceGUID_ReturnsDifferentGuidForDifferentContent() {
    var guid1 = Resource.GetResourceGUID("full address:s:server1");
    var guid2 = Resource.GetResourceGUID("full address:s:server2");

    await Assert.That(guid1).IsNotEqualTo(guid2);
  }

  [Test]
  public async Task GetResourceGUID_SuffixChangesGuid() {
    var content = "full address:s:myserver";

    var guidWithoutSuffix = Resource.GetResourceGUID(content);
    var guidWithSuffix = Resource.GetResourceGUID(content, "Desktop");

    await Assert.That(guidWithoutSuffix).IsNotEqualTo(guidWithSuffix);
  }

  [Test]
  public async Task GetResourceGUID_LinesToOmitAreExcludedFromHash() {
    var contentWith = "full address:s:server1\r\nremoteapplicationname:s:MyApp";
    var contentWithout = "remoteapplicationname:s:MyApp";

    var guidWith = Resource.GetResourceGUID(contentWith, "", ["full address:s:"]);
    var guidWithout = Resource.GetResourceGUID(contentWithout);

    await Assert.That(guidWith).IsEqualTo(guidWithout);
  }

  [Test]
  public async Task GetResourceGUID_ContentWithDifferentLineOrderProducesSameGuid() {
    var content1 = "remoteapplicationname:s:MyApp\r\nfull address:s:myserver";
    var content2 = "full address:s:myserver\r\nremoteapplicationname:s:MyApp";

    var guid1 = Resource.GetResourceGUID(content1);
    var guid2 = Resource.GetResourceGUID(content2);

    await Assert.That(guid1).IsEqualTo(guid2);
  }

  [Test]
  public async Task GuidPropertyIsNullUntilCalculateGuidIsCalled() {
    var resource = MakeResource();

    await Assert.That(resource.Guid).IsNull();
    await Assert.That(resource.Id).IsNull();

    resource.CalculateGuid("full address:s:127.0.0.1", 2.1, true);

    await Assert.That(resource.Guid).IsNotNull();
    await Assert.That(resource.Id).IsNotNull();
  }

  [Test]
  public async Task CalculateGuid_RequiresManuallyProvidedRdpFileContentsWhenResourceOriginIsNotRdpFile() {
    var resource = MakeResource();

    await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => resource.CalculateGuid(2.1, true)));
  }

  [Test]
  public async Task CalculateGuid_ThrowsWhenRdpFileContentsIsNullOrEmpty() {
    var resource = MakeResource();

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => resource.CalculateGuid("", 2.1, true)));
    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => resource.CalculateGuid(null!, 2.1, true)));
  }

  [Test]
  public async Task CalculateGuid_OnlyAddsTitleAsSuffixWhenResourceTypeIsDesktop() {
    var appRdpFileContents = "full address:s:127.0.0.1\r\nremoteapplicationname:s:MyApp";
    var desktopRdpFileContents = "full address:s:127.0.0.1";

    var appRdpFileLocation = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "RemoteApp.rdp");
    var desktopRdpFileLocation = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Desktop.rdp");

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(appRdpFileLocation)!);
      Directory.CreateDirectory(Path.GetDirectoryName(desktopRdpFileLocation)!);
      await File.WriteAllTextAsync(appRdpFileLocation, appRdpFileContents);
      await File.WriteAllTextAsync(desktopRdpFileLocation, desktopRdpFileContents);

      var appResource = MakeResource(source: appRdpFileLocation, origin: ResourceOrigin.Rdp);
      var desktopResource = MakeResource(source: desktopRdpFileLocation, origin: ResourceOrigin.Rdp);

      // we use the same RDP file contents for both, but they should produce different GUIDs
      // because the desktop resource should have the title appended as a suffix
      var appGuid = appResource.CalculateGuid("full address:s:127.0.0.1", 2.1, true);
      var desktopGuid = desktopResource.CalculateGuid("full address:s:127.0.0.1", 2.1, true);

      await Assert.That(appGuid).IsNotEqualTo(desktopGuid);
    }
    finally {
      if (File.Exists(appRdpFileLocation)) {
        File.Delete(appRdpFileLocation);
      }
      if (File.Exists(desktopRdpFileLocation)) {
        File.Delete(desktopRdpFileLocation);
      }
      if (Directory.Exists(Path.GetDirectoryName(appRdpFileLocation)!)) {
        Directory.Delete(Path.GetDirectoryName(appRdpFileLocation)!, true);
      }
      if (Directory.Exists(Path.GetDirectoryName(desktopRdpFileLocation)!)) {
        Directory.Delete(Path.GetDirectoryName(desktopRdpFileLocation)!, true);
      }
    }
  }

  [Test]
  public async Task GetRdpStringProperty_ReturnsValueForMatchingProperty() {
    var rdpContents = "full address:s:myserver\r\nremoteapplicationname:s:MyApp";

    var value = Resource.Utilities.GetRdpStringProperty(rdpContents, "full address:s:");

    await Assert.That(value).IsEqualTo("myserver");
  }

  [Test]
  public async Task GetRdpStringProperty_ReturnsFallbackForMissingProperty() {
    var rdpContents = "full address:s:myserver";

    var value = Resource.Utilities.GetRdpStringProperty(rdpContents, "remoteapplicationname:s:", "DefaultApp");

    await Assert.That(value).IsEqualTo("DefaultApp");
  }

  [Test]
  public async Task GetRdpStringProperty_ReturnsLastInstanceForDuplicateProperty() {
    var rdpContents = "full address:s:first\r\nfull address:s:last";

    var value = Resource.Utilities.GetRdpStringProperty(rdpContents, "full address:s:");

    await Assert.That(value).IsEqualTo("last");
  }

  [Test]
  public async Task GetRdpStringProperty_PreservesColonsInValue() {
    var rdpContents = "full address:s:myserver:3389";

    var value = Resource.Utilities.GetRdpStringProperty(rdpContents, "full address:s:");

    await Assert.That(value).IsEqualTo("myserver:3389");
  }

  [Test]
  public async Task GetRdpStringProperty_ThrowsWhenPropertyNameHasNoColon() {
    var action = () => Resource.Utilities.GetRdpStringProperty("full address:s:myserver", "full address");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task GetRdpFileProperty_ReadsPropertyFromRdpFilePath() {
    var rdpFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Test.rdp");
    var rdpContents = "full address:s:myserver\r\nremoteapplicationname:s:MyApp";

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(rdpFilePath)!);
      await File.WriteAllTextAsync(rdpFilePath, rdpContents);

      var value = Resource.Utilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationname:s:");

      await Assert.That(value).IsEqualTo("MyApp");
    }
    finally {
      if (File.Exists(rdpFilePath)) {
        File.Delete(rdpFilePath);
      }
      if (Directory.Exists(Path.GetDirectoryName(rdpFilePath)!)) {
        Directory.Delete(Path.GetDirectoryName(rdpFilePath)!, true);
      }
    }
  }

  [Test]
  public async Task GetRdpFileProperty_ThrowsFileNotFoundForNonExistentPath() {
    await Assert.ThrowsAsync<FileNotFoundException>(() => Task.Run(() => {
      Resource.Utilities.GetRdpFileProperty("nonexistent_xyz.rdp", "full address:s:");
    }));
  }
}

public class ResourceFromRdpFileTests {
  private string _tempDir = "";

  [Before(Test)]
  public void Setup() {
    _tempDir = Path.Combine(Path.GetTempPath(), "RdpTests_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_tempDir);
  }

  [After(Test)]
  public void Cleanup() {
    if (Directory.Exists(_tempDir)) {
      Directory.Delete(_tempDir, recursive: true);
    }
  }

  private string WriteRdp(string name, string contents) {
    var path = Path.Combine(_tempDir, name);
    File.WriteAllText(path, contents);
    return path;
  }

  [Test]
  public async Task FromRdpFile_ThrowsFullAddressMissingExceptionWhenNoFullAddress() {
    var path = WriteRdp("NoAddress.rdp", "remoteapplicationname:s:MyApp\r\n");

    await Assert.ThrowsAsync<Resource.FullAddressMissingException>(() => Task.Run(() => {
      Resource.FromRdpFile(path);
    }));
  }

  [Test]
  public async Task FromRdpFile_ThrowsArgumentExceptionForInvalidPath() {
    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => {
      Resource.FromRdpFile("");
    }));
  }

  [Test]
  public async Task FromRdpFile_SetsOriginToRdp() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Origin).IsEqualTo(ResourceOrigin.Rdp);
  }

  [Test]
  public async Task FromRdpFile_SetsSourceToRdpFilePath() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Source).IsEqualTo(path);
  }

  [Test]
  public async Task FromRdpFile_SetsFullAddressFromFile() {
    var path = WriteRdp("App.rdp", "full address:s:myserver.raweb.app\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.FullAddress).IsEqualTo("myserver.raweb.app");
  }

  [Test]
  public async Task FromRdpFile_TitleDefaultsToFileNameWithoutExtensionWhenNoRemoteApplicationName() {
    var path = WriteRdp("MyApplication.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Title).IsEqualTo("MyApplication");
  }

  [Test]
  public async Task FromRdpFile_TitleIsReadFromRemoteApplicationNameProperty() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationname:s:My Named Application\r\n";
    var path = WriteRdp("App.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Title).IsEqualTo("My Named Application");
  }

  [Test]
  public async Task FromRdpFile_TypeIsDesktopWhenNoRemoteApplicationProgram() {
    var path = WriteRdp("Desktop.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Type).IsEqualTo(ResourceType.Desktop);
    await Assert.That(resource.IsDesktop).IsTrue();
    await Assert.That(resource.IsApp).IsFalse();
    await Assert.That(resource.AppProgram).IsNull();
  }

  [Test]
  public async Task FromRdpFile_TypeIsRemoteAppWhenRemoteApplicationProgramIsPresent() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationprogram:s:C:\\apps\\myapp.exe\r\n";
    var path = WriteRdp("App.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Type).IsEqualTo(ResourceType.RemoteApp);
    await Assert.That(resource.IsApp).IsTrue();
    await Assert.That(resource.IsDesktop).IsFalse();
  }

  [Test]
  public async Task FromRdpFile_AppProgramIsSetFromRemoteApplicationProgramProperty() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationprogram:s:C:\\apps\\myapp.exe\r\n";
    var path = WriteRdp("App.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.AppProgram).IsEqualTo(@"C:\apps\myapp.exe");
  }

  [Test]
  public async Task FromRdpFile_PipeCharactersAreStrippedFromRemoteApplicationProgramWhenUsedForAppProgram() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationprogram:s:||MyApp\r\n";
    var path = WriteRdp("App.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.AppProgram).IsEqualTo("MyApp");
    await Assert.That(resource.AppProgram!.Contains('|')).IsFalse();
  }

  [Test]
  public async Task FromRdpFile_AppFileExtCsvIsSetFromRemoteApplicationFileExtensionsProperty() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationprogram:s:myapp.exe\r\nremoteapplicationfileextensions:s:.txt,.xlsx\r\n";
    var path = WriteRdp("App.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.AppFileExtCSV).IsEqualTo(".txt,.xlsx");
    await Assert.That(resource.FileExtensions!.Length).IsEqualTo(2);
    await Assert.That(resource.FileExtensions[0]).IsEqualTo(".txt");
    await Assert.That(resource.FileExtensions[1]).IsEqualTo(".xlsx");
  }

  [Test]
  public async Task FromRdpFile_AppFileExtCsvIsIgnoredForDesktopResources() {
    var fileContents = "full address:s:myserver\r\nremoteapplicationfileextensions:s:.txt,.xlsx\r\n";
    var path = WriteRdp("Desktop.rdp", fileContents);

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.AppFileExtCSV).IsNull();
    await Assert.That(resource.FileExtensions!.Length).IsEqualTo(0);
  }

  [Test]
  public async Task FromRdpFile_VirtualFolderArgumentIsUsed() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path, virtualFolder: "Sales/Reports");

    await Assert.That(resource.VirtualFolders.Length).IsEqualTo(1);
    await Assert.That(resource.VirtualFolders[0]).IsEqualTo("Sales/Reports");
  }

  [Test]
  public async Task FromRdpFile_VirtualFolderDefaultsToEmptyString() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.VirtualFolders.Length).IsEqualTo(1);
    await Assert.That(resource.VirtualFolders[0]).IsEqualTo("");
  }

  [Test]
  public async Task FromRdpFile_AliasUsesForwardSlashes() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.Alias.Contains('\\')).IsFalse();
  }

  [Test]
  public async Task FromRdpFile_LastUpdatedIsNotMinValue() {
    var path = WriteRdp("App.rdp", "full address:s:myserver\r\n");

    var resource = Resource.FromRdpFile(path);

    await Assert.That(resource.LastUpdated).IsNotEqualTo(DateTime.MinValue);
  }

  [Test]
  public async Task FromRdpFile_LastUpdatedUsesNewestCompanionFileTimestamp() {
    var rdpPath = WriteRdp("App.rdp", "full address:s:myserver\r\n");
    var iconPath = Path.Combine(_tempDir, "App.ico");
    File.WriteAllText(iconPath, "fake icon");

    // give the icon file a timestamp that is in the future
    var futureTime = DateTime.UtcNow.AddHours(1);
    File.SetLastWriteTimeUtc(rdpPath, DateTime.UtcNow.AddHours(-1));
    File.SetLastWriteTimeUtc(iconPath, futureTime);

    var resource = Resource.FromRdpFile(rdpPath);

    await Assert
      .That(resource.LastUpdated)
      .IsGreaterThanOrEqualTo(futureTime.AddSeconds(-1))
      .Because("the icon file should be considered a companion file and influence the last updated timestamp");
  }

  [Test]
  public async Task FromRdpFile_LastUpdatedUsesRdpTimestampWhenNoNewerCompanionFile() {
    var rdpPath = WriteRdp("App.rdp", "full address:s:myserver\r\n");
    var rdpTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    File.SetLastWriteTimeUtc(rdpPath, rdpTime);

    var resource = Resource.FromRdpFile(rdpPath);

    await Assert.That(resource.LastUpdated).IsEqualTo(rdpTime);
  }

  [Test]
  public async Task FromRdpFile_CompanionFilesMatchByBaseNameOnly() {
    var rdpPath = WriteRdp("App.rdp", "full address:s:myserver\r\n");
    var unrelatedPath = Path.Combine(_tempDir, "Other.ico");
    File.WriteAllText(unrelatedPath, "fake icon");

    var rdpTime = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    var unrelatedTime = DateTime.UtcNow.AddHours(2);
    File.SetLastWriteTimeUtc(rdpPath, rdpTime);
    File.SetLastWriteTimeUtc(unrelatedPath, unrelatedTime);

    var resource = Resource.FromRdpFile(rdpPath);

    // unrelated file should not influence the timestamp.
    await Assert
      .That(resource.LastUpdated)
      .IsEqualTo(rdpTime)
      .Because("the unrelated file should not be considered a companion file and should not influence the last updated timestamp");
  }

  [Test]
  public async Task FromRdpFile_CompanionFilesIncludeDarkModeIconVariants() {
    var rdpPath = WriteRdp("App.rdp", "full address:s:myserver\r\n");
    var iconPath = Path.Combine(_tempDir, "App.png");
    var darkIconPath = Path.Combine(_tempDir, "App-dark.png");
    File.WriteAllText(iconPath, "fake icon");
    File.WriteAllText(darkIconPath, "fake dark icon");

    // give the dark icon file a timestamp that is in the future
    var futureTime = DateTime.UtcNow.AddHours(1);
    File.SetLastWriteTimeUtc(rdpPath, DateTime.UtcNow.AddHours(-1));
    File.SetLastWriteTimeUtc(iconPath, DateTime.UtcNow.AddHours(-1));
    File.SetLastWriteTimeUtc(darkIconPath, futureTime);

    var resource = Resource.FromRdpFile(rdpPath);

    await Assert
      .That(resource.LastUpdated)
      .IsGreaterThanOrEqualTo(futureTime.AddSeconds(-1))
      .Because("the dark mode icon file should be considered a companion file and influence the last updated timestamp");
  }
}
