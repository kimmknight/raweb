using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities.Tests;

public class FileTypeIconNamingTests {
  [Test]
  [Arguments(".xlsx")]
  [Arguments(".XLSX")]
  [Arguments(".tar.gz")]
  [Arguments(".c++")]
  [Arguments(".my_ext-2")]
  public async Task IsValidFileTypeExtension_AcceptsExtensions(string extension) {
    await Assert.That(ManagedFileResource.IsValidFileTypeExtension(extension)).IsTrue();
  }

  [Test]
  [Arguments(null)]
  [Arguments("")]
  [Arguments(".")]
  [Arguments("xlsx")]
  [Arguments("..xlsx")]
  [Arguments(".a..b")]
  [Arguments(".xlsx.")]
  [Arguments("./x")]
  [Arguments(@".x\y")]
  [Arguments(".x!y")]
  [Arguments(".x:y")]
  [Arguments(".x y")]
  public async Task IsValidFileTypeExtension_RejectsUnsafeValues(string? extension) {
    await Assert.That(ManagedFileResource.IsValidFileTypeExtension(extension)).IsFalse();
  }

  [Test]
  public async Task GetFileTypeIconPath_IsLowercaseInsideIconsFolder() {
    await Assert.That(ManagedFileResource.GetFileTypeIconEntryPath(".XLSX")).IsEqualTo("icons/.xlsx.png");
  }

  [Test]
  public async Task GetFileTypeIconPath_ThrowsForInvalidExtension() {
    await Assert.That(() => ManagedFileResource.GetFileTypeIconEntryPath("../x")).Throws<ArgumentException>();
  }

  [Test]
  [Arguments("MyApp", "MyApp", null)]
  [Arguments("MyApp!.xlsx", "MyApp", ".xlsx")]
  [Arguments("MyApp.resource:.xlsx", "MyApp.resource", ".xlsx")]
  [Arguments("My!App", "My!App", null)]
  [Arguments("A!B!.docx", "A!B", ".docx")]
  [Arguments("Report!", "Report!", null)]
  public async Task ParseImageReference_SplitsResourceNameAndExtension(string reference, string expectedName, string? expectedExtension) {
    ManagedFileResource.ParseImageReference(reference, out var resourceName, out var extension);

    await Assert.That(resourceName).IsEqualTo(expectedName);
    await Assert.That(extension).IsEqualTo(expectedExtension);
  }
}

public class FileTypeIconDecodingTests {
  internal static readonly byte[] s_png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

  [Test]
  public async Task DecodeFileTypeIcons_DecodesPngAndRemoval() {
    var icons = ManagedFileResource.DecodeBase64FileTypeIcons(new Dictionary<string, string> {
      [".xlsx"] = Convert.ToBase64String(s_png),
      [".docx"] = "",
    });

    await Assert.That(icons[".xlsx"]).IsEquivalentTo(s_png);
    await Assert.That(icons[".docx"]).IsNull();
  }

  [Test]
  public async Task DecodeFileTypeIcons_RejectsNonPngImages() {
    var notPng = Convert.ToBase64String("GIF89a"u8.ToArray());

    await Assert.That(() => ManagedFileResource.DecodeBase64FileTypeIcons(new Dictionary<string, string> { [".xlsx"] = notPng }))
      .Throws<ArgumentException>();
  }

  [Test]
  public async Task DecodeFileTypeIcons_RejectsInvalidBase64() {
    await Assert.That(() => ManagedFileResource.DecodeBase64FileTypeIcons(new Dictionary<string, string> { [".xlsx"] = "not base64!" }))
      .Throws<ArgumentException>();
  }

  [Test]
  public async Task DecodeFileTypeIcons_RejectsInvalidExtension() {
    await Assert.That(() => ManagedFileResource.DecodeBase64FileTypeIcons(new Dictionary<string, string> { ["../../evil"] = "" }))
      .Throws<ArgumentException>();
  }
}

public class FileTypeIconStorageTests {
  private string _folder = "";

  [Before(Test)]
  public void Setup() {
    // the folder must be named managed-resources so that image lookups treat the files as managed resources
    _folder = Path.Combine(Path.GetTempPath(), "raweb-tests-" + Guid.NewGuid().ToString("N"), "managed-resources");
    Directory.CreateDirectory(_folder);
  }

  [After(Test)]
  public void Cleanup() {
    var root = Path.GetDirectoryName(_folder)!;
    if (Directory.Exists(root)) {
      foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)) {
        File.SetAttributes(path, FileAttributes.Normal);
      }
      File.SetAttributes(_folder, FileAttributes.Normal);
      Directory.Delete(root, true);
    }
  }

  private ManagedFileResource CreateResource(string extensions) {
    return new ManagedFileResource(
      rootedFilePath: Path.Combine(_folder, "MyApp.resource"),
      name: "My App",
      rdpFileString: $"full address:s:server\r\nremoteapplicationprogram:s:||MyApp\r\nremoteapplicationfileextensions:s:{extensions}\r\n",
      iconPath: null,
      iconIndex: 0,
      includeInWorkspace: true,
      virtualFolders: null
    );
  }

  private static Dictionary<string, byte[]?> Icons(params (string Extension, byte[]? Bytes)[] icons) {
    return icons.ToDictionary(icon => icon.Extension, icon => icon.Bytes);
  }

  [Test]
  public async Task WriteToFile_StoresIconAndReportsItOnReload() {
    var resource = CreateResource(".xlsx,.docx");
    resource.SetPendingFileTypeIcons(Icons((".xlsx", FileTypeIconDecodingTests.s_png)));

    resource.WriteToFile();
    var reloaded = ManagedFileResource.FromResourceFile(resource.RootedFilePath);
    var associations = reloaded.RemoteAppProperties!.FileTypeAssociations;

    await Assert.That(associations.Count).IsEqualTo(2);
    await Assert.That(associations.Single(a => a.Extension == ".xlsx").IconPath).IsEqualTo("./icons/.xlsx.png");
    await Assert.That(associations.Single(a => a.Extension == ".docx").IconPath).IsEqualTo("");
  }

  [Test]
  public async Task ReadImageStream_ReturnsFileTypeIcon() {
    var resource = CreateResource(".xlsx");
    resource.SetPendingFileTypeIcons(Icons((".XLSX", FileTypeIconDecodingTests.s_png)));
    resource.WriteToFile();

    using var stream = ManagedFileResource.FromResourceFile(resource.RootedFilePath).ReadImageStream(out var iconPath, fileTypeAssociation: ".xlsx");

    await Assert.That(iconPath).IsEqualTo("icons/.xlsx.png");
    await Assert.That(stream.ToArray()).IsEquivalentTo(FileTypeIconDecodingTests.s_png);
  }

  [Test]
  public async Task ReadImageStream_ThrowsWhenFileTypeHasNoIcon() {
    var resource = CreateResource(".xlsx");
    resource.WriteToFile();

    await Assert.That(() => resource.ReadImageStream(out _, fileTypeAssociation: ".xlsx")).Throws<FileNotFoundException>();
  }

  [Test]
  public async Task WriteToFile_RemovesIconWhenRequested() {
    var resource = CreateResource(".xlsx");
    resource.SetPendingFileTypeIcons(Icons((".xlsx", FileTypeIconDecodingTests.s_png)));
    resource.WriteToFile();

    resource.SetPendingFileTypeIcons(Icons((".xlsx", null)));
    resource.WriteToFile();

    var reloaded = ManagedFileResource.FromResourceFile(resource.RootedFilePath);
    await Assert.That(reloaded.RemoteAppProperties!.FileTypeAssociations.Single().IconPath).IsEqualTo("");
  }

  [Test]
  public async Task WriteToFile_RemovesIconsForExtensionsThatAreNoLongerAssociated() {
    var resource = CreateResource(".xlsx,.docx");
    resource.SetPendingFileTypeIcons(Icons((".xlsx", FileTypeIconDecodingTests.s_png), (".docx", FileTypeIconDecodingTests.s_png)));
    resource.WriteToFile();

    // remove .xlsx from the associations
    resource.RemoteAppProperties = new RemoteAppProperties(
      applicationPath: resource.RemoteAppProperties!.ApplicationPath,
      commandLine: null,
      commandLineOption: RemoteAppProperties.CommandLineMode.Optional,
      fileTypeAssociations: new RemoteAppProperties.FileTypeAssociationCollection([new RemoteAppProperties.FileTypeAssociation(".docx", "")])
    );
    resource.RdpFileString = resource.ToRdpFileStringBuilder().ToString();
    resource.WriteToFile();

    using var archive = System.IO.Compression.ZipFile.OpenRead(resource.RootedFilePath);
    await Assert.That(archive.GetEntry("icons/.xlsx.png")).IsNull();
    await Assert.That(archive.GetEntry("icons/.docx.png")).IsNotNull();
  }

  private static RemoteAppProperties WithExtensions(RemoteAppProperties properties, params string[] extensions) {
    return new RemoteAppProperties(
      applicationPath: properties.ApplicationPath,
      commandLine: properties.CommandLine,
      commandLineOption: properties.CommandLineOption,
      fileTypeAssociations: new RemoteAppProperties.FileTypeAssociationCollection(
        extensions.Select(extension => new RemoteAppProperties.FileTypeAssociation(extension, ""))
      )
    );
  }

  /// <summary>
  /// Editing an existing resource (like the PATCH endpoint does) must save the new file extensions
  /// in the RDP file, along with their icons.
  /// </summary>
  [Test]
  public async Task ModifyingRemoteAppProperties_SavesNewExtensionsAndIconsForExistingResource() {
    CreateResource(".xlsx").WriteToFile();
    var existing = ManagedFileResource.FromResourceFile(Path.Combine(_folder, "MyApp.resource"));

    existing.RemoteAppProperties = WithExtensions(existing.RemoteAppProperties!, ".xlsx", ".txt");
    existing.RdpFileString = existing.ToRdpFileStringBuilder().ToString();
    existing.SetPendingFileTypeIcons(Icons((".txt", FileTypeIconDecodingTests.s_png)));
    existing.WriteToFile();

    var reloaded = ManagedFileResource.FromResourceFile(existing.RootedFilePath);
    var associations = reloaded.RemoteAppProperties!.FileTypeAssociations;
    await Assert.That(associations.Select(a => a.Extension)).IsEquivalentTo([".xlsx", ".txt"]);
    await Assert.That(associations.Single(a => a.Extension == ".txt").IconPath).IsEqualTo("./icons/.txt.png");
  }

  /// <summary>
  /// The file extensions are covered by RDP signing, so a signed RDP file keeps its extensions,
  /// and an icon for an extension that could not be saved is not kept.
  /// </summary>
  [Test]
  public async Task ModifyingRemoteAppProperties_DoesNotChangeExtensionsOfSignedRdpFile() {
    var signed = new ManagedFileResource(
      rootedFilePath: Path.Combine(_folder, "Signed.resource"),
      name: "Signed",
      rdpFileString: "full address:s:server\r\nremoteapplicationprogram:s:||Signed\r\nsignscope:s:Full Address,RemoteApplicationProgram\r\nsignature:s:AAAA\r\n",
      iconPath: null,
      iconIndex: 0,
      includeInWorkspace: true,
      virtualFolders: null
    );
    signed.WriteToFile();

    signed.RemoteAppProperties = WithExtensions(signed.RemoteAppProperties!, ".txt");
    signed.RdpFileString = signed.ToRdpFileStringBuilder().ToString();
    signed.SetPendingFileTypeIcons(Icons((".txt", FileTypeIconDecodingTests.s_png)));
    signed.WriteToFile();

    var reloaded = ManagedFileResource.FromResourceFile(signed.RootedFilePath);
    await Assert.That(reloaded.RdpFileString).Contains("signature:s:AAAA");
    await Assert.That(reloaded.RemoteAppProperties!.FileTypeAssociations.Count).IsEqualTo(0);
    using var archive = System.IO.Compression.ZipFile.OpenRead(signed.RootedFilePath);
    await Assert.That(archive.GetEntry("icons/.txt.png")).IsNull();
  }

  [Test]
  public async Task Constructor_AcceptsSemicolonSeparatedExtensionsFromOlderFiles() {
    var resource = CreateResource(".xlsx; .docx");

    await Assert.That(resource.RemoteAppProperties!.FileTypeAssociations.Select(a => a.Extension)).IsEquivalentTo([".xlsx", ".docx"]);
  }

  [Test]
  public async Task ImagePathToStream_ServesFileTypeIconForImageEndpoints() {
    var resource = CreateResource(".xlsx");
    resource.SetPendingFileTypeIcons(Icons((".xlsx", FileTypeIconDecodingTests.s_png)));
    resource.WriteToFile();

    var result = ImageUtilities.ImagePathToStream(resource.RootedFilePath, ".xlsx");

    await Assert.That(result.ImagePath).IsEqualTo(resource.RootedFilePath);
    await Assert.That(result.ImageStream!.ToArray()).IsEquivalentTo(FileTypeIconDecodingTests.s_png);
  }

  [Test]
  public async Task ToResourceFile_IncludesFileTypeIcons() {
    var resource = CreateResource(".xlsx");
    resource.SetPendingFileTypeIcons(Icons((".xlsx", FileTypeIconDecodingTests.s_png)));
    resource.WriteToFile();

    var exported = ManagedFileResource.FromResourceFile(resource.RootedFilePath).ToResourceFile();
    using var archive = exported.archive;

    await Assert.That(archive.GetEntry("icons/.xlsx.png")).IsNotNull();
  }
}
