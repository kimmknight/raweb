using System.Xml.Linq;

namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public partial class WorkspaceBuilderTests {
  private static readonly XNamespace s_tswf = "http://schemas.microsoft.com/ts/2007/05/tswf";

  private string _tempFile = "";
  private string _tempAppRoot = "";

  /// <summary>
  /// Creates an isolated app root, policies file, and default test resource for each test run.
  /// </summary>
  [Before(Test)]
  public void Setup() {
    // isolate each test run under a unique app root.
    _tempAppRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempAppRoot);
    Constants.AppRoot = _tempAppRoot;
    AppId.Initialize();

    _tempFile = Path.GetTempFileName();
    File.Delete(_tempFile);
    // create an empty policies store for this test environment.
    _ = new PoliciesManager(_tempFile);

    var resourcesDir = Path.Combine(Constants.AppDataFolderPath, "resources");
    Directory.CreateDirectory(resourcesDir);

    // write a test desktop
    File.WriteAllText(
      Path.Combine(resourcesDir, "TestDesktop.rdp"),
      "full address:s:myserver.example.com\r\n"
    );

    // write a test remoteapp
    File.WriteAllText(
      Path.Combine(resourcesDir, "TestRemoteApp.rdp"),
      "full address:s:myserver.example.com\r\nremoteapplicationmode:i:1\r\nremoteapplicationprogram:s:||notepad\r\n"
    );

    // write a remoteapp with file type associations
    File.WriteAllText(
      Path.Combine(resourcesDir, "TestRemoteAppWithFileExtensions.rdp"),
      "full address:s:myserver2.example.com\r\nremoteapplicationmode:i:1\r\nremoteapplicationprogram:s:||wordpad\r\nremoteapplicationfileextensions:s:.txt,.rtf\r\n"
    );
  }

  /// <summary>
  /// Deletes temporary test files and directories created during setup.
  /// </summary>
  [After(Test)]
  public void Cleanup() {
    if (File.Exists(_tempFile)) {
      File.Delete(_tempFile);
    }
    if (Directory.Exists(_tempAppRoot)) {
      Directory.Delete(_tempAppRoot, true);
    }
  }

  private static readonly UserInformation s_testUser = new("S-1-5-21-1000", "jdoe", "DOMAIN");


  /// <summary>
  /// Parses a workspace XML string into an XDocument.
  /// </summary>
  private static XDocument Parse(string xml) => XDocument.Parse(xml);
  /// <summary>
  /// Returns the root element of the parsed workspace XML document.
  /// For MS-TSWP, this is the ResourceCollection element.
  /// </summary>
  private static XElement Root(XDocument doc) => doc.Root!;
  /// <summary>
  /// Returns the Publisher element from the workspace XML document.
  /// </summary>
  private static XElement Publisher(XDocument doc) => Root(doc).Element(s_tswf + "Publisher")!;
  /// <summary>
  /// Returns the Resources element from the workspace XML document.
  /// </summary>
  private static XElement Resources(XDocument doc) => Publisher(doc).Element(s_tswf + "Resources")!;
  /// <summary>
  /// Returns the TerminalServers element from the workspace XML document.
  /// </summary>
  private static XElement TerminalServers(XDocument doc) => Publisher(doc).Element(s_tswf + "TerminalServers")!;
  /// <summary>
  /// Enumerates Resource elements from the workspace XML document.
  /// </summary>
  private static IEnumerable<XElement> ResourceElements(XDocument doc) => Resources(doc).Elements(s_tswf + "Resource");
  private static XElement TestDesktopResource(XDocument doc) => ResourceElements(doc).First(r => r.Attribute("Alias")!.Value == "resources/TestDesktop.rdp");
  private static XElement TestRemoteAppResource(XDocument doc) => ResourceElements(doc).First(r => r.Attribute("Alias")!.Value == "resources/TestRemoteApp.rdp");
  private static XElement TestRemoteAppWithFileExtensionsResource(XDocument doc) => ResourceElements(doc).First(r => r.Attribute("Alias")!.Value == "resources/TestRemoteAppWithFileExtensions.rdp");
  private static readonly string[] s_expectedFileExtensionsForTestRemoteAppWithFileExtensions = [".txt", ".rtf"];

  /// <summary>
  /// Builds a workspace document for each schema version and invokes the corresponding
  /// callback. Omit a version's callback to skip assertions for that version.
  /// </summary>
  private static async Task Matrix(
    Func<XDocument, Task>? v1_1 = null,
    Func<XDocument, Task>? v2 = null,
    Func<XDocument, Task>? v2_1 = null,
    UserInformation? user = null,
    string fqdn = "example.com",
    bool? mergeTerminalServers = null,
    string? terminalServerFilter = null,
    string iisBase = "/",
    Management.IManagementServiceDirectClient? managedResourceService = null,
    string? resourcesFolder = null
  ) {
    user ??= UserInformation.AnonymousUser;
    (WorkspaceBuilder.SchemaVersion version, Func<XDocument, Task>? assertions)[] versions = [
      (WorkspaceBuilder.SchemaVersion.v1_1, v1_1),
      (WorkspaceBuilder.SchemaVersion.v2, v2),
      (WorkspaceBuilder.SchemaVersion.v2_1, v2_1),
    ];
    foreach (var (version, assertions) in versions) {
      if (assertions is null) {
        continue;
      }
      var builder = new WorkspaceBuilder(
        version,
        user,
        fqdn,
        mergeTerminalServers ?? false,
        terminalServerFilter,
        iisBase,
        managedResourceService
      );
      var doc = resourcesFolder is null
        ? Parse(builder.GetWorkspaceXmlString())
        : Parse(builder.GetWorkspaceXmlString(resourcesFolder: resourcesFolder));
      Console.WriteLine();
      Console.WriteLine($"Workspace XML for schema version {version}:\n{doc}");
      Console.WriteLine("--------------------------------------------------");
      Console.WriteLine(doc);
      Console.WriteLine("--------------------------------------------------");
      Console.WriteLine();
      await assertions(doc);
    }
  }

  /// <summary>
  /// Returns true if <paramref name="s"/> is a valid xs:dateTime string.
  /// </summary>
  private static bool IsValidXsDateTime(string s) {
    try {
      System.Xml.XmlConvert.ToDateTimeOffset(s);
      return true;
    }
    catch (FormatException) {
      return false;
    }
  }


  /// <summary>
  /// Verifies that Constructor Throws For Unknown Schema Version.
  /// </summary>
  [Test]
  public async Task Constructor_ThrowsForUnknownSchemaVersion() {
    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => {
      _ = new WorkspaceBuilder((WorkspaceBuilder.SchemaVersion)99, s_testUser, "example.com");
    }));
  }


  /// <summary>
  /// Verifies that Get Workspace Xml String Produces Valid Xml.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ProducesValidXml() {
    static async Task Assertion(XDocument doc) {
      // if invalid XML, the Matrix will throw before we get here
      // because Matrix will experience an XmlException when trying
      // to parse invalid XML
      await Assert.That(doc.Root).IsNotNull().Because("GetWorkspaceXmlString should produce valid XML that can be parsed into an XDocument.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }


  /// <summary>
  /// Verifies that Get Workspace Xml String Has Correct Xmlns.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_HasCorrectXmlns() {
    static async Task Assertion(XDocument doc) {
      var xmlns = Root(doc).Name.NamespaceName;
      await Assert.That(xmlns).IsEqualTo("http://schemas.microsoft.com/ts/2007/05/tswf");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that Get Workspace Xml String Has Pub Date Attribute.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_HasPubDateAttribute() {
    static async Task Assertion(XDocument doc) {
      var pubDateAttribute = Root(doc).Attribute("PubDate");
      await Assert.That(pubDateAttribute).IsNotNull().Because("Root element should have a PubDate attribute.");
      await Assert.That(pubDateAttribute!.Value).IsNotEmpty().Because("PubDate attribute should have a non-empty value.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that the workspace publish date is in a valid format
  /// and is the current date (with some leeway for test execution time).
  [Test]
  public async Task GetWorkspaceXmlString_PubDateMatchesExpectedFormat() {
    static async Task Assertion(XDocument doc) {
      var pubDateValue = Root(doc).Attribute("PubDate")?.Value;
      if (pubDateValue is null) {
        Assert.Fail("Root element should have a PubDate attribute.");
        return;
      }

      // validate that it's a valid xs:dateTime string
      var pubDateIsValid = IsValidXsDateTime(pubDateValue);
      await Assert.That(pubDateIsValid).IsTrue().Because("PubDate should be a valid xs:dateTime string.");

      // validate that it's within 5 minutes of the current time
      var pubDate = System.Xml.XmlConvert.ToDateTimeOffset(pubDateValue).UtcDateTime;
      var now = DateTime.UtcNow;
      var fiveMinutesAgo = now.AddMinutes(-5);
      var fiveMinutesFromNow = now.AddMinutes(5);
      var pubDateIsCurrent = pubDate >= fiveMinutesAgo && pubDate <= fiveMinutesFromNow;
      Console.WriteLine($"PubDate: {pubDate}, Now: {now}, FiveMinutesAgo: {fiveMinutesAgo}, FiveMinutesFromNow: {fiveMinutesFromNow}");
      await Assert.That(pubDateIsCurrent).IsTrue().Because("PubDate should be within 5 minutes of the current time.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> uses SchemaVersion 1.1.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V1_SchemaVersionAttributeIs1Point1() {
    await Matrix(
      v1_1: async doc => {
        var schemaVersion = Root(doc).Attribute("SchemaVersion")!.Value;
        await Assert.That(schemaVersion).IsEqualTo("1.1");
      }
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> uses SchemaVersion 2.0.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_SchemaVersionAttributeIs2Point0() {
    await Matrix(
      v2: async doc => {
        var schemaVersion = Root(doc).Attribute("SchemaVersion")!.Value;
        await Assert.That(schemaVersion).IsEqualTo("2");
      }
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> uses SchemaVersion 2.1.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_1_SchemaVersionAttributeIs2Point1() {
    await Matrix(
      v2_1: async doc => {
        var schemaVersion = Root(doc).Attribute("SchemaVersion")!.Value;
        await Assert.That(schemaVersion).IsEqualTo("2.1");
      }
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> has a SupportsReconnect attribute value
  /// set to false when using SchemaVersion v2 or higher.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_HasSupportsReconnectFalse() {
    static async Task Assertion(XDocument doc) {
      var supportsReconnect = Root(doc).Attribute("SupportsReconnect")!.Value;
      await Assert.That(supportsReconnect).IsEqualTo("false");
    }

    await Matrix(
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> does not have a SupportsReconnect
  /// attribute when using SchemaVersion v1.1.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V1_DoesNotHaveSupportsReconnectAttribute() {
    await Matrix(
      v1_1: async doc => {
        await Assert.That(Root(doc).Attribute("SupportsReconnect")).IsNull();
      }
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> does not have a DisplayFolder attribute.
  /// This attribute, which is supported in MS-TSWP v2.0 and later, is not supported
  /// by RAWeb and should never be present. It is intended to indicate that the XML only
  /// includes a singular folder from a larger workspace.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_DoesNotHaveDisplayFolderAttribute() {
    static async Task Assertion(XDocument doc) {
      await Assert.That(Root(doc).Attribute("DisplayFolder")).IsNull();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }


  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a Publisher element with
  /// an ID attribute that matches the server's fully-qualified domain name.
  /// </summary>
  /// <remarks>
  /// MS-TSWP allows ID to be a GUID or a FQDN. RAWeb chooses to use the FQDN since we can
  /// more easily guarantee consistency. If we used a GUID, we would need to persist a file,
  /// but that file could be lost if RAWeb is temporarily uninstalled or if RAWeb is moved
  /// to a different server.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_PublisherIdMatchesFqdn() {
    static async Task Assertion(XDocument doc) {
      var publisherId = Publisher(doc).Attribute("ID")!.Value;
      await Assert.That(publisherId).IsEqualTo("raweb.example.com");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      fqdn: "raweb.example.com"
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a Publisher
  /// element with an empty Description attribute.
  /// </summary>
  /// <remarks>
  /// Publisher descriptions are optional. RAWeb does not support populating
  /// publisher descriptions.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_PublisherDescriptionIsEmpty() {
    static async Task Assertion(XDocument doc) {
      var description = Publisher(doc).Attribute("Description")!.Value;
      await Assert.That(description).IsEqualTo("");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a Publisher
  /// element with a LastUpdated attribute that is a valid timestamp.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_PublisherHasValidLastUpdatedAttribute() {
    static async Task Assertion(XDocument doc) {
      // must be a valid xs:dateTime string
      var lastUpdated = Publisher(doc).Attribute("LastUpdated")!.Value;
      await Assert.That(IsValidXsDateTime(lastUpdated)).IsTrue();

      // must be the same as the newest resource's LastUpdated attribute
      var newestResourceLastUpdated = ResourceElements(doc)
        .Select(r => DateTime.Parse(r.Attribute("LastUpdated")!.Value))
        .Max();
      var publisherLastUpdated = DateTime.Parse(lastUpdated);
      await Assert.That(publisherLastUpdated).IsEqualTo(newestResourceLastUpdated);
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a Publisher
  /// element with a Name attribute.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_PublisherHasNameAttribute() {
    static async Task Assertion(XDocument doc) {
      var name = Publisher(doc).Attribute("Name")!.Value;
      await Assert.That(name).IsNotEmpty().Because("Publisher should have a non-empty Name attribute.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a
  /// Resources element.
  /// </summary>
  /// <remarks>
  /// MS-TSWP requires a Resources element, even if it is empty.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ContainsResourcesSection() {
    static async Task Assertion(XDocument doc) {
      await Assert.That(Publisher(doc).Element(s_tswf + "Resources")).IsNotNull();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with a
  /// TerminalServers element.
  /// </summary>
  /// <remarks>
  /// MS-TSWP requires a Resources element, even if it is empty.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ContainsTerminalServersSection() {
    static async Task Assertion(XDocument doc) {
      await Assert.That(Publisher(doc).Element(s_tswf + "TerminalServers")).IsNotNull();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  [Test]
  public async Task GetWorkspaceXmlString_TerminalServersHaveUniqueIds() {
    static async Task Assertion(XDocument doc) {
      var terminalServers = TerminalServers(doc).Elements(s_tswf + "TerminalServer");
      var ids = terminalServers.Select(ts => ts.Attribute("ID")!.Value);

      var idsAreNotNullOrEmpty = ids.All(id => !string.IsNullOrEmpty(id));
      await Assert.That(idsAreNotNullOrEmpty).IsTrue().Because("Every TerminalServer element should have a non-empty ID attribute.");

      var uniqueIds = ids.Distinct();
      await Assert.That(ids.Count()).IsEqualTo(uniqueIds.Count()).Because("Every TerminalServer element should have a unique ID attribute.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  [Test]
  public async Task GetWorkspaceXmlString_TerminalServersHaveFQDN() {
    static async Task Assertion(XDocument doc) {
      var terminalServers = TerminalServers(doc).Elements(s_tswf + "TerminalServer");
      var fqdns = terminalServers.Select(ts => ts.Attribute("ID")?.Value);

      var fqdnsAreNotNullOrEmpty = fqdns.All(fqdn => !string.IsNullOrEmpty(fqdn));
      await Assert.That(fqdnsAreNotNullOrEmpty).IsTrue().Because("Every TerminalServer element should have a non-empty Name attribute.");

      var fqdnsAreValid = fqdns.All(fqdn => Uri.CheckHostName(fqdn) != UriHostNameType.Unknown);
      await Assert.That(fqdnsAreValid).IsTrue().Because("Every TerminalServer element should have a Name attribute that is a valid fully-qualified domain name (FQDN).");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  [Test]
  public async Task GetWorkspaceXmlString_TerminalServersHaveCorrectLastUpdatedTimestamp() {
    static async Task Assertion(XDocument doc) {
      var terminalServers = TerminalServers(doc).Elements(s_tswf + "TerminalServer");
      await Assert.That(terminalServers).IsNotEmpty().Because("There should be at least one TerminalServer element to validate.");

      foreach (var ts in terminalServers) {
        // validate date format
        var lastUpdated = ts.Attribute("LastUpdated")?.Value;
        var lastUpdatedIsValidDate = !string.IsNullOrEmpty(lastUpdated) && IsValidXsDateTime(lastUpdated);
        await Assert.That(lastUpdatedIsValidDate).IsTrue().Because("Every TerminalServer element should have a non-empty LastUpdated attribute that is a valid xs:dateTime string.");

        // make sure the date is correct for the resources associated with this terminal server
        var relatedResources = ResourceElements(doc).Where(resource => {
          var terminalServerRef = resource
            .Element(s_tswf + "HostingTerminalServers")?
            .Element(s_tswf + "HostingTerminalServer")?
            .Element(s_tswf + "TerminalServerRef");
          return terminalServerRef != null && terminalServerRef.Attribute("Ref")?.Value == ts.Attribute("ID")?.Value;
        });
        var relatedResourcesList = relatedResources.ToList();
        foreach (var resource in relatedResourcesList) {
          var resourceLastUpdatedAttr = resource.Attribute("LastUpdated")?.Value;
          await Assert.That(resourceLastUpdatedAttr).IsNotNull().Because("Every Resource element should have a LastUpdated attribute.");
        }
        var newestRelatedResourceLastUpdated = relatedResourcesList.Max(resource => DateTime.Parse(resource.Attribute("LastUpdated")!.Value));
        var terminalServerLastUpdated = DateTime.Parse(lastUpdated!);
        await Assert.That(terminalServerLastUpdated).IsEqualTo(newestRelatedResourceLastUpdated).Because("Every TerminalServer element's LastUpdated attribute should be the same as the newest LastUpdated attribute of any Resource elements that reference it.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with an empty
  /// Resources element when a terminal server filter is applied that filters out all resources.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_TerminalServerFilterProducesEmptyResourcesSection() {
    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc)).IsEmpty();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      terminalServerFilter: "nonexistentserver.example.com"
    );
  }

  /// <summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns equivalent XML when the
  /// terminal server filter is set to null or an empty string.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_NullAndEmptyTerminalServerFilterAreEquivalent() {
    var withNull = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com", terminalServerFilter: null);
    var withEmpty = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com", terminalServerFilter: "");

    await Assert.That(withNull.GetWorkspaceXmlString()).IsEqualTo(withEmpty.GetWorkspaceXmlString());
  }

  ///<summary>
  /// Verifies that <cref="GetWorkspaceXmlString"/> returns xml with the correct
  /// Resource elements when the terminal server filter is applied.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_TerminalServerFilterProducesExpectedResources() {
    // one of the three test resources that are always generated for each test
    // is for myserver2.example.com
    static async Task Assertion(XDocument doc) {

      // only one resource should match the filter
      var resources = ResourceElements(doc);
      await Assert.That(resources.Count()).IsEqualTo(1).Because("Only one resource should be returned when filtering for myserver2.example.com.");

      // the referenced terminal server should be myserver2.example.com
      // and that terminal server should exist as a TerminalServer element
      var resource = resources.First();
      var terminalServerRef = resource.Element(s_tswf + "HostingTerminalServers")?.Element(s_tswf + "HostingTerminalServer")?.Element(s_tswf + "TerminalServerRef");
      await Assert.That(terminalServerRef).IsNotNull().Because("Resource should have a TerminalServerRef element.");
      var refValue = terminalServerRef!.Attribute("Ref")?.Value;
      var terminalServer = TerminalServers(doc).Elements(s_tswf + "TerminalServer").FirstOrDefault(ts => ts.Attribute("ID")!.Value == refValue);
      await Assert.That(terminalServer).IsNotNull().Because("TerminalServerRef should reference a valid TerminalServer ID.");
      var terminalServerFqdn = terminalServer!.Attribute("ID")?.Value;
      await Assert.That(terminalServerFqdn).IsEqualTo("myserver2.example.com").Because("TerminalServerRef should reference the terminal server that matches the filter, myserver2.example.com.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      terminalServerFilter: "myserver2.example.com"
    );
  }

  /// <summary>
  /// Verifies that the number of Resource elements matches the number of RDP files in the test setup.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_WithRdpFile_ContainsOneResource() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      await Assert.That(resources.Count()).IsEqualTo(3);
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Resource element has a unique ID attribute.
  /// </summary>
  /// <remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceHasIdAttribute() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var ids = resources.Select(r => r.Attribute("ID")!.Value);

      var idsAreNotNullOrEmpty = ids.All(id => !string.IsNullOrEmpty(id));
      await Assert.That(idsAreNotNullOrEmpty).IsTrue().Because("Every Resource element should have a non-empty ID attribute.");

      var uniqueIds = ids.Distinct();
      await Assert.That(ids.Count()).IsEqualTo(uniqueIds.Count()).Because("Every Resource element should have a unique ID attribute.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Resource has an Alias attribute that is unqiue to
  /// the containing Resources element.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceAliasIsUniqueToResourcesElement() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var aliases = resources.Select(r => r.Attribute("Alias")!.Value);

      var aliasesAreNotNullOrEmpty = aliases.All(alias => !string.IsNullOrEmpty(alias));
      await Assert.That(aliasesAreNotNullOrEmpty).IsTrue().Because("Every Resource element should have a non-empty Alias attribute.");

      var groupedByResourcesElement = resources.GroupBy(r => r.Parent);
      foreach (var group in groupedByResourcesElement) {
        var groupAliases = group.Select(r => r.Attribute("Alias")!.Value);
        var uniqueGroupAliases = groupAliases.Distinct();
        await Assert.That(groupAliases.Count()).IsEqualTo(uniqueGroupAliases.Count()).Because("Every Resource element within the same Resources element should have a unique Alias attribute.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Resource Alias attribute is one of:
  /// - a relative path to the RDP file in the resources directory
  /// - a realtive path to the RDP file in the multiuser-resources directory
  /// - a relative path to the .resource file in the managed-resources directory
  /// - the name of the registry key, prefixed with registry/, for registry resources
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceAliasIsExpectedFormatForRAWeb() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var aliases = resources.Select(r => r.Attribute("Alias")!.Value);

      string[] expectedPrefixes = [
        "resources/",
        "multiuser-resources/",
        "managed-resources/",
        "registry/"
      ];
      var aliasHasExpectedPrefix = aliases.All(alias => expectedPrefixes.Any(prefix => alias.StartsWith(prefix)));
      await Assert.That(aliasHasExpectedPrefix).IsTrue().Because("Every Resource Alias should be a relative path to a file in the resources, multiuser-resources, or managed-resources directories, or a registry key name prefixed with registry/.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every resource as a non-empty title attribute.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceTitleExists() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var titles = resources.Select(r => r.Attribute("Title")?.Value);

      var titlesAreNotNullOrEmpty = titles.All(title => !string.IsNullOrEmpty(title));
      await Assert.That(titlesAreNotNullOrEmpty).IsTrue().Because("Every Resource element should have a non-empty Title attribute.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Resource has a valid LastUpdated attribute
  /// that is an xs:dateTime string.
  /// </summary>
  /// <remarks>
  /// MS-TSWP does not require this attribute, but it mandates that
  /// if it is present, it must be a valid xs:dateTime string. RAWeb includes
  /// this attribute on all resources.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceHasLastUpdatedAttribute() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var lastUpdatedValues = resources.Select(r => r.Attribute("LastUpdated")?.Value);

      var allResourcesHaveLastUpdated = lastUpdatedValues.All(v => !string.IsNullOrEmpty(v));
      await Assert.That(allResourcesHaveLastUpdated).IsTrue().Because("Every Resource element should have a non-empty LastUpdated attribute.");

      var allLastUpdatedValuesAreValidDates = lastUpdatedValues.All(v => IsValidXsDateTime(v!));
      await Assert.That(allLastUpdatedValuesAreValidDates).IsTrue().Because("Every Resource LastUpdated attribute should be a valid xs:dateTime string.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every resource has a Type attribute with value "Desktop" or "RemoteApp".
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceTypeIsDesktopForRdpWithNoRemoteApp() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var types = resources.Select(r => r.Attribute("Type")?.Value);

      var typesAreValid = types.All(t => t == "Desktop" || t == "RemoteApp");
      await Assert.That(typesAreValid).IsTrue().Because("Every Resource element should have a Type attribute with value Desktop or RemoteApp.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Resource has a ShowByDefault attribute with value "true" when
  /// the schema version is v2.1 or higher.
  /// </summary>
  /// <remarks>
  /// MS-TSWP schema version 2.1 introduced the SHowByDefault attribute, but
  /// the default value for it is true. Clients are supposed to treat missing
  /// ShowByDefault attributes as if they were set to true, but RAWeb always
  /// includes the attribute with an explicit value of true for clarity.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceShouldShowByDefault() {
    static async Task PositiveAssertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var showByDefaultValues = resources.Select(r => r.Attribute("ShowByDefault")?.Value);

      var allResourcesHaveShowByDefaultTrue = showByDefaultValues.All(v => v == "True");
      await Assert.That(allResourcesHaveShowByDefaultTrue).IsTrue().Because("Every Resource element should have a ShowByDefault attribute with value true.");
    }

    static async Task NegativeAssertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var showByDefaultAttributes = resources.Select(r => r.Attribute("ShowByDefault"));

      var allResourcesHaveShowByDefault = showByDefaultAttributes.All(a => a != null);
      await Assert.That(allResourcesHaveShowByDefault).IsFalse().Because("Resource elements should not have a ShowByDefault attribute when the schema version is less than 2.1.");
    }

    await Matrix(
      v1_1: NegativeAssertion,
      v2: NegativeAssertion,
      v2_1: PositiveAssertion
    );
  }

  /// <summary>
  /// Verifies that every Resource element has an Icons element.
  /// </summary>
  /// <remarks>
  /// MS-TSWP does not require the Icons element, but RAWeb always
  /// includes a default icon if no icon is available.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceHasIconsElement() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var iconsElements = resources.Select(r => r.Element(s_tswf + "Icons"));

      var allResourcesHaveIcons = iconsElements.All(e => e != null);
      await Assert.That(allResourcesHaveIcons).IsTrue().Because("Every Resource element should have an Icons element.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every Icons element contains an IconRaw element.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceIconsContainsIconRaw() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var iconsElements = resources.Select(r => r.Element(s_tswf + "Icons"));
      var iconRawElements = iconsElements.Select(e => e?.Element(s_tswf + "IconRaw"));

      var allIconsHaveIconRaw = iconRawElements.All(e => e != null);
      await Assert.That(allIconsHaveIconRaw).IsTrue().Because("Every Icons element should contain an IconRaw element.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that Get Workspace Xml String Icon Raw Has File Type Ico.
  /// </summary>
  /// <remarks>
  /// MS-TWSP does not necessarily require .ico format for IconRaw elements,
  /// but it is the only icon element that supports multi-size icons, and
  /// .ico is the only format that RAWeb supports for multi-size icons.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_IconRawHasFileTypeIco() {
    static async Task Assertion(XDocument doc) {
      var allResources = ResourceElements(doc);
      var iconRawElements = allResources
        .Select(r => r.Element(s_tswf + "Icons"))
        .Select(e => e?.Element(s_tswf + "IconRaw"));

      var allFileTypesAreIco = iconRawElements.All(e => e?.Attribute("FileType")?.Value == "Ico");
      await Assert.That(allFileTypesAreIco).IsTrue().Because("Every IconRaw element should have a FileType attribute with value ico.");

      // require it to actuall be ico; must include format=ico in the URL query string
      var allFileUrlsContainFormatIco = iconRawElements.All(element => {
        var fileUrl = element?.Attribute("FileURL")?.Value;
        if (string.IsNullOrEmpty(fileUrl)) {
          return false;
        }
        var queryString = fileUrl.Contains('?') ? fileUrl.Substring(fileUrl.IndexOf('?')) : "";
        var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);
        Console.WriteLine($"FileURL: {fileUrl}, QueryString: {queryString}, format param: {queryParams["format"]}");
        return queryParams["format"] == "ico";
      });
      await Assert.That(allFileUrlsContainFormatIco).IsTrue().Because("Every IconRaw FileURL should include format=ico in the query string.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that Get Workspace Xml String Icon Sequence Is Followed.
  /// </summary>
  /// <remarks>
  /// MS-TSWP specifies that if multiple icon types are provided, clients should use them
  /// in the order. If a specific size is missing, that is permitted.
  [Test]
  public async Task GetWorkspaceXmlString_ResourceIconSequenceIsFollowed() {
    string[] sequence = [
      "IconRaw",
      "Icon16",
      "Icon32",
      "Icon48",
      "Icon64",
      "Icon100",
      "Icon256"
    ];

    async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var iconsElement = resource.Element(s_tswf + "Icons");
        if (iconsElement is null) {
          continue;
        }

        var lastIndex = -1;
        foreach (var iconType in sequence) {
          var iconElement = iconsElement.Element(s_tswf + iconType);
          if (iconElement is not null) {
            var currentIndex = Array.IndexOf(sequence, iconType);
            var previousIconType = lastIndex >= 0 ? sequence[lastIndex] : "(none)";
            await Assert.That(currentIndex).IsGreaterThan(lastIndex).Because($"Icon types should be provided in the order specified by MS-TSWP. {iconType} was found after {previousIconType}.");
            lastIndex = currentIndex;
          }
        }
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every icon with a fixed size specifies its dimensions
  /// in the Dimensions attribute. The width and height must be equal and
  /// must correspond to the size specified in the element name (e.g. Icon16
  /// should have dimensions of 16x16).
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceIconMustSpecifyDimensionsUnlessRaw() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var iconsElement = resource.Element(s_tswf + "Icons");
        if (iconsElement is null) {
          continue;
        }

        var iconElements = iconsElement.Elements().Where(e => e.Name.LocalName != "IconRaw");
        foreach (var iconElement in iconElements) {
          var dimensions = iconElement.Attribute("Dimensions")?.Value;
          await Assert.That(dimensions).IsNotNull().Because($"Icon element {iconElement.Name.LocalName} must have a Dimensions attribute.");

          var dimensionParts = dimensions!.Split('x');
          await Assert.That(dimensionParts.Length).IsEqualTo(2).Because($"Icon element {iconElement.Name.LocalName} has invalid Dimensions format. Expected format is {{width}}x{{height}}.");

          var width = dimensionParts[0];
          var height = dimensionParts[1];
          await Assert.That(width).IsEqualTo(height).Because($"Icon element {iconElement.Name.LocalName} must have equal width and height.");

          var expectedSize = iconElement.Name.LocalName.Replace("Icon", "");
          await Assert.That(width).IsEqualTo(expectedSize).Because($"Icon element {iconElement.Name.LocalName} has mismatched size and Dimensions attribute. Expected size is {expectedSize}x{expectedSize}.");
        }
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifes that every Icon element specifies a FileURL attribute.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceIconsHaveFileUrl() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var iconsElements = resources.Select(r => r.Element(s_tswf + "Icons"));
      var iconElements = iconsElements.SelectMany(e => e?.Elements().Where(e => e.Attribute("FileURL") != null) ?? []);

      var allIconsHaveFileUrl = iconElements.All(e => e.Attribute("FileURL") != null);
      await Assert.That(allIconsHaveFileUrl).IsTrue().Because("Every icon element should have a FileURL attribute.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }


  /// <summary>
  /// Verifies that every Resource element has a FileExtensions element. It may be empty.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceHasFileExtensionsElementWhenAppropriate() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      var fileExtensionsElements = resources.Select(r => r.Element(s_tswf + "FileExtensions"));

      var allResourcesHaveFileExtensions = fileExtensionsElements.All(e => e != null);
      await Assert.That(allResourcesHaveFileExtensions).IsTrue().Because("Every Resource element should have a FileExtensions element.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that resources with no file extensions have an empty FileExtensions element with no children.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceWithNoFileExtensions_FileExtensionsHasNoChildren() {
    static async Task Assertion(XDocument doc) {
      var resource = TestRemoteAppResource(doc);
      var fileExtensions = resource.Element(s_tswf + "FileExtensions");

      await Assert.That(fileExtensions).IsNotNull().Because("Resource elements should have a FileExtensions element, even if there are no file extensions.");
      await Assert.That(fileExtensions!.Elements(s_tswf + "FileExtension")).IsEmpty().Because("Resources with no file extensions should have an empty FileExtensions element with no children.");

      resource = TestDesktopResource(doc);
      fileExtensions = resource.Element(s_tswf + "FileExtensions");

      await Assert.That(fileExtensions).IsNotNull().Because("Resource elements should have a FileExtensions element, even if there are no file extensions.");
      await Assert.That(fileExtensions!.Elements(s_tswf + "FileExtension")).IsEmpty().Because("Resources with no file extensions should have an empty FileExtensions element with no children.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that resources with file extensions have a FileExtensions element
  /// with a FileExtension child for each file extension specified in the RDP file.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_RemoteAppWithFileExtensions_FileExtensionsHasChildrenWithValidName() {
    static async Task Assertion(XDocument doc) {
      var resource = TestRemoteAppWithFileExtensionsResource(doc);

      var fileExtensions = resource.Element(s_tswf + "FileExtensions");
      await Assert.That(fileExtensions).IsNotNull().Because("Resource elements should have a FileExtensions element.");

      var fileExtensionElements = fileExtensions!.Elements(s_tswf + "FileExtension");
      await Assert.That(fileExtensionElements).IsNotEmpty().Because("Resources with file extensions should have a FileExtensions element with FileExtension children.");
      await Assert.That(fileExtensionElements.Count()).IsEqualTo(2).Because("Resource elements should have a FileExtension child for each file extension specified in the RDP file.");

      foreach (var expectedExt in s_expectedFileExtensionsForTestRemoteAppWithFileExtensions) {
        var matchingExtElement = fileExtensionElements.FirstOrDefault(e => e.Attribute("Name")?.Value == expectedExt);
        await Assert.That(matchingExtElement).IsNotNull().Because($"There should be a FileExtension element with Name attribute value {expectedExt} for each file extension specified in the RDP file.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that FileExtension elements have a PrimaryHandler attribute with value "True"
  /// when using SchemaVersion v2 or higher.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_FileExtensionHasPrimaryHandlerTrue() {
    static async Task PositiveAssertion(XDocument doc) {
      var resource = TestRemoteAppWithFileExtensionsResource(doc);
      var fileExtensions = resource.Element(s_tswf + "FileExtensions");
      var fileExtensionElements = fileExtensions!.Elements(s_tswf + "FileExtension");

      var primaryHandlerAttributes = fileExtensionElements.Select(e => e.Attribute("PrimaryHandler")?.Value);
      var allPrimaryHandlersAreTrue = primaryHandlerAttributes.All(v => v == "True");
      await Assert.That(allPrimaryHandlersAreTrue).IsTrue().Because("Every FileExtension element should have a PrimaryHandler attribute with value True when using SchemaVersion v2 or higher.");
    }

    static async Task NegativeAssertion(XDocument doc) {
      var resource = TestRemoteAppWithFileExtensionsResource(doc);
      var fileExtensions = resource.Element(s_tswf + "FileExtensions");
      var fileExtensionElements = fileExtensions!.Elements(s_tswf + "FileExtension");

      var primaryHandlerAttributes = fileExtensionElements.Select(e => e.Attribute("PrimaryHandler"));
      var allPrimaryHandlersAreNull = primaryHandlerAttributes.All(a => a == null);
      await Assert.That(allPrimaryHandlersAreNull).IsTrue().Because("FileExtension elements should not have a PrimaryHandler attribute when using SchemaVersion v1.1.");
    }

    await Matrix(
      v1_1: NegativeAssertion,
      v2: PositiveAssertion,
      v2_1: PositiveAssertion
    );
  }

  [Test]
  public async Task GetWorkspaceXmlString_EveryResourceHasFoldersElement() {
    static async Task PositiveAssertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var foldersElement = resource.Element(s_tswf + "Folders");
        await Assert.That(foldersElement).IsNotNull().Because("Every Resource element should have a Folders element.");
      }
    }

    static async Task NegativeAssertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var foldersElement = resource.Element(s_tswf + "Folders");
        await Assert.That(foldersElement).IsNull().Because("Resource elements should not have a Folders element when using SchemaVersion v1.1.");
      }
    }

    await Matrix(
      v1_1: NegativeAssertion,
      v2: PositiveAssertion,
      v2_1: PositiveAssertion
    );
  }

  [Test]
  public async Task GetWorkspaceXmlString_ResourceFoldersElementHasValidFolderChildren() {
    static async Task PositiveAssertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var foldersElement = resource.Element(s_tswf + "Folders");
        var folderElements = foldersElement?.Elements(s_tswf + "Folder");

        await Assert.That(folderElements).IsNotNull().Because("Every Resource element should have a Folders element with Folder children.");
        await Assert.That(folderElements).IsNotEmpty().Because("Every Resource element should have at least one Folder child under the Folders element.");

        // every child should be an empty element with a Name attribute
        foreach (var folderElement in folderElements!) {
          await Assert.That(folderElement.HasElements).IsFalse().Because("Folder elements should not have any child elements.");
          var nameAttribute = folderElement.Attribute("Name");
          await Assert.That(nameAttribute).IsNotNull().Because("Folder elements should have a Name attribute.");
          await Assert.That(nameAttribute!.Value).IsNotEmpty().Because("Folder Name attributes should not be empty.");
        }
      }
    }

    await Matrix(
      v2: PositiveAssertion,
      v2_1: PositiveAssertion
    );
  }

  /// <summary>
  /// Verifies that every Resource has a HostingTerminalServers element with at least
  /// one HostingTerminalServer child.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceHasHostingTerminalServersElement() {
    static async Task Assertion(XDocument doc) {
      var resources = ResourceElements(doc);
      foreach (var resource in resources) {
        var hostingTerminalServers = resource.Element(s_tswf + "HostingTerminalServers");
        await Assert.That(hostingTerminalServers).IsNotNull().Because("Every Resource element should have a HostingTerminalServers element.");

        var hostingTerminalServerElements = hostingTerminalServers!.Elements(s_tswf + "HostingTerminalServer");
        await Assert.That(hostingTerminalServerElements).IsNotEmpty().Because("Every HostingTerminalServers element should have at least one HostingTerminalServer child.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies every HostingTerminalServer element has a ResourceFile child with
  /// a FileExtension attribute with value ".rdp".
  /// </summary>
  /// <remarks>
  /// Per the MS-TSWP specification, a ResourceFile can be any file extension.
  /// For RDP resources, the specification requires the .rdp file extension
  /// for resources that use the terminal services client (i.e. RemoteApp and
  /// Desktop resources).
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceFileHasDotRdpExtension() {
    static async Task Assertion(XDocument doc) {
      var hostingTerminalServers = doc.Descendants(s_tswf + "HostingTerminalServer");
      foreach (var hostingTerminalServer in hostingTerminalServers) {
        var resourceFile = hostingTerminalServer.Element(s_tswf + "ResourceFile");
        await Assert.That(resourceFile).IsNotNull().Because("Every HostingTerminalServer element should have a ResourceFile child.");

        var fileExtension = resourceFile!.Attribute("FileExtension")?.Value;
        await Assert.That(fileExtension).IsEqualTo(".rdp").Because("The ResourceFile element for RDP resources should have a FileExtension attribute with value .rdp.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every HostingTerminalServer element has a ResourceFile child
  /// with a URL attribute.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ResourceFileHasUrlAttribute() {
    static async Task Assertion(XDocument doc) {
      var hostingTerminalServers = doc.Descendants(s_tswf + "HostingTerminalServer");
      foreach (var hostingTerminalServer in hostingTerminalServers) {
        var resourceFile = hostingTerminalServer.Element(s_tswf + "ResourceFile");
        await Assert.That(resourceFile).IsNotNull().Because("Every HostingTerminalServer element must have a ResourceFile child.");

        var url = resourceFile!.Attribute("URL")?.Value;
        await Assert.That(url).IsNotNull().Because("The ResourceFile element for RDP resources must have a URL attribute.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }

  /// <summary>
  /// Verifies that every HostingTerminalServer element has a TerminalServerRef child with a Ref attribute
  /// that matches the ID of a TerminalServer element in the TerminalServers element.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_TerminalServerRefIsInTerminalServersList() {
    static async Task Assertion(XDocument doc) {
      var terminalServerIds = TerminalServers(doc)
        .Elements(s_tswf + "TerminalServer")
        .Select(ts => ts.Attribute("ID")!.Value)
        .ToHashSet();

      var terminalServerRefs = doc.Descendants(s_tswf + "TerminalServerRef");
      foreach (var terminalServerRef in terminalServerRefs) {
        var refId = terminalServerRef.Attribute("Ref")?.Value;
        await Assert.That(refId).IsNotNull().Because("Every TerminalServerRef element should have a Ref attribute.");
        await Assert.That(terminalServerIds.Contains(refId!)).IsTrue().Because($"Every TerminalServerRef element should reference a TerminalServer element in the TerminalServers list. No TerminalServer with ID {refId} was found.");
      }
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion
    );
  }





































  [Test]
  public async Task GetWorkspaceXmlString_WithoutResources_PublisherLastUpdatedIsMinValue() {
    var emptyDir = Path.Combine(_tempAppRoot, "empty-resources");
    Directory.CreateDirectory(emptyDir);

    static async Task Assertion(XDocument doc) {
      await Assert.That(Publisher(doc).Attribute("LastUpdated")!.Value).IsEqualTo("0001-01-01T00:00:00Z");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      resourcesFolder: emptyDir
    );
  }

  /// <summary>
  /// Verifies that identical RDP files in separate folders produce a single Resource
  /// element with a Folder element for each folder.
  /// This only applies to schema version 2.0 or higher.
  /// </summary>
  /// <remarks>
  /// RAWeb generates GUIDs for RDP resources based on the contents of the RDP file. If the file
  /// is identical, the GUID will be identical.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_V2_IdenticalRdpInSubdir_ProducesOneResourceWithBothFolders() {
    var dir = Path.Combine(_tempAppRoot, "rdp-dedup-v2");
    var subdir = Path.Combine(dir, "subdir");
    Directory.CreateDirectory(subdir);
    var content = "full address:s:myserver.example.com\r\n";
    File.WriteAllText(Path.Combine(dir, "App.rdp"), content);
    File.WriteAllText(Path.Combine(subdir, "App.rdp"), content);

    static async Task Assertion(XDocument doc) {
      // check that both folders appear
      await Assert.That(ResourceElements(doc).Count()).IsEqualTo(1);
      var folders = ResourceElements(doc).First().Element(s_tswf + "Folders")!.Elements(s_tswf + "Folder");
      await Assert.That(folders.Count()).IsEqualTo(2);

      // check that the folder names are correct
      var folderNames = folders.Select(f => f.Attribute("Name")!.Value).ToList();
      await Assert.That(folderNames.Contains("/")).IsTrue();
      await Assert.That(folderNames.Contains("/subdir")).IsTrue();
    }

    await Matrix(
      v2: Assertion,
      v2_1: Assertion,
      resourcesFolder: dir
    );
  }

  /// <summary>
  /// Verifies that identical RDP files in separate folders produce separate Resource
  /// elements when using schema version 1.1.
  /// </summary>
  /// <remarks>
  /// MS-TSWP schema versions 1.1 does not support folders.
  /// RAWeb includes the virtual folder when computing the GUID for RDP resources in
  /// schema version 1.1 so that no deduplication ever occurs.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_V1_IdenticalRdpInSubdir_ProducesTwoResources() {
    var dir = Path.Combine(_tempAppRoot, "rdp-dedup-v1");
    var subdir = Path.Combine(dir, "subdir");
    Directory.CreateDirectory(subdir);
    var content = "full address:s:myserver.example.com\r\n";
    File.WriteAllText(Path.Combine(dir, "App.rdp"), content);
    File.WriteAllText(Path.Combine(subdir, "App.rdp"), content);

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Count()).IsEqualTo(2);
    }

    await Matrix(
      v1_1: Assertion,
      resourcesFolder: dir
    );
  }


  /// <summary>
  /// Verifies that RDP file GUID generation ignores the "full address:s:" and other related lines
  /// when mergeTerminalServers is true so that the same RemoteApp on different servers
  /// appears as a single Resource with multiple HostingTerminalServers instead of multiple Resources.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_MergeTerminalServersTrue_SameRemoteAppOnTwoServers_ProducesOneResource() {
    // When merging is on, "full address:s:" is excluded from the GUID hash for RemoteApps,
    // so the same app on two servers produces a single merged resource.
    var dir = Path.Combine(_tempAppRoot, "rdp-merge-true");
    Directory.CreateDirectory(dir);
    File.WriteAllText(
      Path.Combine(dir, "App1.rdp"),
      "full address:s:server1.example.com\r\nremoteapplicationprogram:s:myapp.exe\r\n"
    );
    File.WriteAllText(
      Path.Combine(dir, "App2.rdp"),
      "full address:s:server2.example.com\r\nremoteapplicationprogram:s:myapp.exe\r\n"
    );

    static async Task Assertion(XDocument doc) {
      // verify that only one resource is produced
      await Assert.That(ResourceElements(doc).Count()).IsEqualTo(1);

      // verify that the resource has two HostingTerminalServer children
      var hostingTerminalServers = ResourceElements(doc).First()
        .Element(s_tswf + "HostingTerminalServers")!
        .Elements(s_tswf + "HostingTerminalServer");
      await Assert.That(hostingTerminalServers.Count()).IsEqualTo(2);
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      mergeTerminalServers: true,
      resourcesFolder: dir
    );
  }

  /// <summary>
  /// Verifies that when mergeTerminalServers is false, the same RemoteApp on different
  /// servers produces separate Resources, each with one HostingTerminalServer.
  /// </summary>
  /// <remarks>
  /// When the mergeTerminalServers option is false, the full address is included in the
  /// GUID hash for RemoteApps, which causes the same app on different servers to produce
  /// separate resources.
  /// </remarks>
  [Test]
  public async Task GetWorkspaceXmlString_MergeTerminalServersFalse_SameRemoteAppOnTwoServers_ProducesTwoResources() {
    var dir = Path.Combine(_tempAppRoot, "rdp-merge-false");
    Directory.CreateDirectory(dir);
    File.WriteAllText(
      Path.Combine(dir, "App1.rdp"),
      "full address:s:server1.example.com\r\nremoteapplicationprogram:s:myapp.exe\r\n"
    );
    File.WriteAllText(
      Path.Combine(dir, "App2.rdp"),
      "full address:s:server2.example.com\r\nremoteapplicationprogram:s:myapp.exe\r\n"
    );

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Count()).IsEqualTo(2);
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      mergeTerminalServers: false,
      resourcesFolder: dir
    );
  }

  /// <summary>
  /// Confirms that the mergeTerminalServers option only applies to RemoteApps, not Desktops.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_MergeTerminalServersTrue_DesktopResources_AreNotMergedAcrossServers() {
    var dir = Path.Combine(_tempAppRoot, "rdp-merge-desktop");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "Desk1.rdp"), "full address:s:server1.example.com\r\n");
    File.WriteAllText(Path.Combine(dir, "Desk2.rdp"), "full address:s:server2.example.com\r\n");

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Count()).IsEqualTo(2);
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      mergeTerminalServers: true,
      resourcesFolder: dir
    );
  }

  /// <summary>
  /// Verifies that multiuser resources for the current user appear in the workspace XML.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_MultiuserResourceForCurrentUser_AppearsInWorkspace() {
    // ProcessMultiuserResources looks for files under multiuser-resources/user/{username}/.
    // AnonymousUser has username="anonymous".
    var userFolder = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "user", "anonymous");
    Directory.CreateDirectory(userFolder);
    File.WriteAllText(Path.Combine(userFolder, "UserApp.rdp"), "full address:s:multiuser.example.com\r\n");

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).Contains("UserApp");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that multiuser resources for a group of which the current user is a member
  /// appears in the workspace XML.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_MultiuserResourceForGroup_AppearsInWorkspace() {
    // ProcessMultiuserResources also looks under multiuser-resources/group/{groupSid}/.
    // AnonymousUser is in Everyone (S-1-1-0).
    var groupFolder = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "group", "S-1-1-0");
    Directory.CreateDirectory(groupFolder);
    File.WriteAllText(Path.Combine(groupFolder, "GroupApp.rdp"), "full address:s:groupserver.example.com\r\n");

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).Contains("GroupApp");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that managed resources with includeInWorkspace=true appear in the workspace XML.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ManagedResource_AppearsInWorkspace() {
    // ProcessManagedResources reads .resource files from managed-resources/.
    var managedDir = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    Directory.CreateDirectory(managedDir);
    new Management.ManagedFileResource(
      Path.Combine(managedDir, "ManagedApp.resource"),
      null,
      "full address:s:managed.example.com\r\nremoteapplicationname:s:Managed App\r\n",
      iconPath: null,
      iconIndex: null,
      includeInWorkspace: true,
      virtualFolders: null,
      securityDescriptor: null
    ).WriteToFile();

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).Contains("Managed App");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that the ResourceFile element's URL attribute for managed resources
  /// includes the "?from=mr" query string to indicate that the resource came from
  /// a managed resource. Without this query parameter, RAWeb's resource file api
  /// endpoint will not know how to find the correct source file for the resource.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ManagedResource_ResourceFileUrlContainsFromMrQueryString() {
    var managedDir = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    Directory.CreateDirectory(managedDir);
    new Management.ManagedFileResource(
      Path.Combine(managedDir, "ManagedApp2.resource"),
      null,
      "full address:s:managed.example.com\r\nremoteapplicationname:s:Managed App 2\r\n",
      iconPath: null,
      iconIndex: null,
      includeInWorkspace: true,
      virtualFolders: null,
      securityDescriptor: null
    ).WriteToFile();

    static async Task Assertion(XDocument doc) {
      var url = ResourceElements(doc)
        .First(r => r.Attribute("Title")!.Value == "Managed App 2")
        .Element(s_tswf + "HostingTerminalServers")!
        .Element(s_tswf + "HostingTerminalServer")!
        .Element(s_tswf + "ResourceFile")!
        .Attribute("URL")!.Value;
      await Assert.That(url.Contains("?from=mr")).IsTrue();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that managed resources with security descriptors that do not explicitly allow
  /// the user do not appear in the workspace XML. If users should implicitly have access
  /// to a resource, the resource should not have a security descriptor. Lack of explicit
  /// access is interpreted as a denial of access.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ManagedResourceWithSecurityDescriptorDenyingUser_DoesNotAppear() {
    var managedDir = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    Directory.CreateDirectory(managedDir);
    // If we only allow administrators, the anonymous user will not be able to see this resource.
    new Management.ManagedFileResource(
      Path.Combine(managedDir, "RestrictedApp.resource"),
      null,
      "full address:s:restricted.example.com\r\nremoteapplicationname:s:Restricted App\r\n",
      iconPath: null,
      iconIndex: null,
      includeInWorkspace: true,
      virtualFolders: null,
      securityDescriptor: new System.Security.AccessControl.RawSecurityDescriptor("D:(A;;0x1;;;S-1-5-32-544)")
    ).WriteToFile();

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).DoesNotContain("Restricted App");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that managed resources with includeInWorkspace=false do not appear in the workspace XML.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_ManagedResourceWithIncludeInWorkspaceFalse_DoesNotAppear() {
    var managedDir = Path.Combine(Constants.AppDataFolderPath, "managed-resources");
    Directory.CreateDirectory(managedDir);
    new Management.ManagedFileResource(
      Path.Combine(managedDir, "HiddenApp.resource"),
      null,
      "full address:s:hidden.example.com\r\nremoteapplicationname:s:Hidden App\r\n",
      iconPath: null,
      iconIndex: null,
      includeInWorkspace: false,
      virtualFolders: null,
      securityDescriptor: null
    ).WriteToFile();

    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).DoesNotContain("Hidden App");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// RAWeb offers functionality to use an alias for a terminal server instead of
  /// its actual address. When an alias is configured for a terminal server,
  /// it should be used in place of the Publisher Name attribute.
  /// </summary>
  /// <returns></returns>
  [Test]
  public async Task GetWorkspaceXmlString_PublisherNameResolvesThroughAlias() {
    PoliciesManager.Set("TerminalServerAliases", "myserver.example.com=Friendly Name");

    static async Task Assertion(XDocument doc) {
      await Assert
        .That(Publisher(doc).Attribute("Name")!.Value)
        .IsEqualTo("Friendly Name")
        .Because("The Publisher Name should prefer an available alias.");
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      terminalServerFilter: "myserver.example.com"
    );
  }

  /// <summary>
  /// Verifies that when ShowMultiuserResourcesUserAndGroupNames is true (the default),
  /// multiuser resources are placed in virtual folders named after the user or group.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_MultiuserGroupResource_VirtualFolderIsGroupName() {
    var groupFolder = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "group", "Everyone");
    Directory.CreateDirectory(groupFolder);
    File.WriteAllText(Path.Combine(groupFolder, "GroupApp.rdp"), "full address:s:groupserver.example.com\r\n");

    static async Task Assertion(XDocument doc) {
      var folderNames = ResourceElements(doc).First(r => r.Attribute("Title")!.Value == "GroupApp")
        .Element(s_tswf + "Folders")!
        .Elements(s_tswf + "Folder")
        .Select(f => f.Attribute("Name")!.Value);
      await Assert.That(folderNames).Contains("/Everyone");
      await Assert.That(folderNames).DoesNotContain("/");
    }

    await Matrix(
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// Verifies that when ShowMultiuserResourcesUserAndGroupNames is false,
  /// multiuser resources are placed in the root virtual folder and
  /// user or group names are not used.
  /// </summary>
  [Test]
  public async Task GetWorkspaceXmlString_V2_MultiuserGroupResource_ShowNamesFalse_VirtualFolderIsRoot() {
    PoliciesManager.Set("Workspace.ShowMultiuserResourcesUserAndGroupNames", "false");
    var groupFolder = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "group", "Everyone");
    Directory.CreateDirectory(groupFolder);
    File.WriteAllText(Path.Combine(groupFolder, "GroupApp.rdp"), "full address:s:groupserver.example.com\r\n");

    static async Task Assertion(XDocument doc) {
      var folderNames = ResourceElements(doc).First(r => r.Attribute("Title")!.Value == "GroupApp")
        .Element(s_tswf + "Folders")!
        .Elements(s_tswf + "Folder")
        .Select(f => f.Attribute("Name")!.Value);
      await Assert.That(folderNames).Contains("/");
      await Assert.That(folderNames).DoesNotContain("/Everyone");
    }

    await Matrix(
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser
    );
  }

  /// <summary>
  /// If the management service's AreConnectionsAllowed method throws an
  /// exception (e.g. because the management service is not running),
  /// the workspace builder should treat the error as an indication that
  /// terminal server connections on the host server are disabled and skip
  /// showing the resources that require registry access. In that case, the
  /// resources not shown in the registry should still be included in the
  /// worskpace XML.
  /// </summary>
  /// <returns></returns>
  [Test]
  public async Task GetWorkspaceXmlString_AreConnectionsAllowedThrows_DoesNotCrashAndSkipsRegistry() {
    // should still create workspace XML from the file-based resources created in Setup().
    static async Task Assertion(XDocument doc) {
      await Assert.That(ResourceElements(doc)).IsNotEmpty();
    }

    await Matrix(
      v1_1: Assertion,
      v2: Assertion,
      v2_1: Assertion,
      user: UserInformation.AnonymousUser,
      managedResourceService: new ThrowingSubManagementService()
    );
  }

  private sealed class ThrowingSubManagementService : Management.IManagementServiceDirectClient {
    public bool AreConnectionsAllowed() => throw new InvalidOperationException("connection check failed");
    public void InitializeRegistryPaths(string? collectionName = null) { }
    public void InitializeDesktopRegistryPaths(string collectionName) { }
    public void RestorePackagedAppIconPaths(string? collectionName) { }
    public void WriteRemoteAppToRegistry(Management.SystemRemoteApps.SystemRemoteApp app) { }
    public void DeleteRemoteAppFromRegistry(Management.SystemRemoteApps.SystemRemoteApp app) { }
    public Management.InstalledApps ListInstalledApps(string? userSid = null) => new([]);
    public void WriteDesktopToRegistry(Management.SystemDesktop desktop) { }
    public void DeleteDesktopFromRegistry(Management.SystemDesktop desktop) { }
    public Stream GetWallpaperStream(Management.SystemDesktop desktop, Management.ManagedFileResource.ImageTheme theme, string? userSid) => Stream.Null;
  }

  private sealed class StubManagementService : Management.IManagementServiceDirectClient {
    public bool AreConnectionsAllowed() => true;
    public void InitializeRegistryPaths(string? collectionName = null) { }
    public void InitializeDesktopRegistryPaths(string collectionName) { }
    public void RestorePackagedAppIconPaths(string? collectionName) { }
    public void WriteRemoteAppToRegistry(Management.SystemRemoteApps.SystemRemoteApp app) { }
    public void DeleteRemoteAppFromRegistry(Management.SystemRemoteApps.SystemRemoteApp app) { }
    public Management.InstalledApps ListInstalledApps(string? userSid = null) => new([]);
    public void WriteDesktopToRegistry(Management.SystemDesktop desktop) { }
    public void DeleteDesktopFromRegistry(Management.SystemDesktop desktop) { }
    public Stream GetWallpaperStream(Management.SystemDesktop desktop, Management.ManagedFileResource.ImageTheme theme, string? userSid) => Stream.Null;
  }
}

// ----------------------------------------------------------------------------
// Tests and test helpers related to the registry-based resources
// ----------------------------------------------------------------------------

public partial class WorkspaceBuilderTests {
  private const string TSAppAllowListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";

  /// <summary>
  /// Builds the CentralPublishedResources registry path for a collection.
  /// </summary>
  private static string CentralizedPublishingAppsPath(string collectionName) =>
    $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\Applications";

  /// <summary>
  /// Writes a test registry RemoteApp entry to both the TSAppAllowList and CentralizedPublishing
  /// registry locations.
  /// </summary>
  /// <param name="collectionName"></param>
  /// <param name="appName"></param>
  /// <param name="showValue">0 = do not show; 1 = show</param>
  /// <param name="displayName"></param>
  private static void WriteRegistryApp(string collectionName, string appName, int showValue, string displayName) {
    void WriteToPath(string path, string showKey) {
      using var appsKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path, writable: true);
      using var appKey = appsKey?.CreateSubKey(appName);
      appKey?.SetValue("Name", displayName);
      appKey?.SetValue("Path", @"C:\Windows\System32\notepad.exe");
      appKey?.SetValue("VPath", @"C:\Windows\System32\notepad.exe");
      appKey?.SetValue("CommandLineSetting", 1);
      appKey?.SetValue("IconIndex", 0);
      appKey?.SetValue(showKey, showValue);
    }
    WriteToPath(TSAppAllowListPath, "ShowInTSWA");
    WriteToPath(CentralizedPublishingAppsPath(collectionName), "ShowInPortal");
  }

  /// <summary>
  /// Deletes a test registry RemoteApp entry from both registry locations.
  /// </summary>
  private static void DeleteRegistryApp(string collectionName, string appName) {
    using var ts = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(TSAppAllowListPath, writable: true);
    ts?.DeleteSubKeyTree(appName, throwOnMissingSubKey: false);
    using var cp = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(CentralizedPublishingAppsPath(collectionName), writable: true);
    cp?.DeleteSubKeyTree(appName, throwOnMissingSubKey: false);
  }

  [Test]
  public async Task GetWorkspaceXmlString_RegistryResource_AppearsInWorkspace() {
    await Assert
      .That(Management.ElevatedPrivileges.Check())
      .IsTrue()
      .Because("creating registry entries requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var appName = "RAWebWBTest_" + Guid.NewGuid().ToString("N")[..8];
    try {
      WriteRegistryApp(collectionName, appName, 1, "Registry App");

      static async Task Assertion(XDocument doc) {
        await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).Contains("Registry App");
      }

      await Matrix(
        v1_1: Assertion,
        v2: Assertion,
        v2_1: Assertion,
        user: UserInformation.AnonymousUser,
        managedResourceService: new StubManagementService()
      );
    }
    finally {
      DeleteRegistryApp(collectionName, appName);
    }
  }

  [Test]
  public async Task GetWorkspaceXmlString_RegistryResource_ResourceFileUrlContainsFromRegistryQueryString() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue()
      .Because("creating registry entries requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var appName = "RAWebWBTest_" + Guid.NewGuid().ToString("N")[..8];
    try {
      WriteRegistryApp(collectionName, appName, 1, "Registry App URL");

      static async Task Assertion(XDocument doc) {
        var url = ResourceElements(doc).First(r => r.Attribute("Title")!.Value == "Registry App URL")
          .Element(s_tswf + "HostingTerminalServers")!
          .Element(s_tswf + "HostingTerminalServer")!
          .Element(s_tswf + "ResourceFile")!
          .Attribute("URL")!.Value;
        await Assert.That(url.Contains("?from=registry")).IsTrue();
      }

      await Matrix(
        v1_1: Assertion,
        v2: Assertion,
        v2_1: Assertion,
        user: UserInformation.AnonymousUser,
        managedResourceService: new StubManagementService()
      );
    }
    finally {
      DeleteRegistryApp(collectionName, appName);
    }
  }

  [Test]
  public async Task GetWorkspaceXmlString_RegistryResourceNotIncludedInWorkspace_IsSkipped() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue()
      .Because("creating registry entries requires HKLM write access");

    var collectionName = AppId.ToCollectionName();
    var appName = "RAWebWBTest_" + Guid.NewGuid().ToString("N")[..8];
    try {
      // ShowInPortal=0 means the app is not flagged for the workspace, so it is skipped.
      WriteRegistryApp(collectionName, appName, 0, "Hidden Reg App");

      static async Task Assertion(XDocument doc) {
        await Assert.That(ResourceElements(doc).Select(r => r.Attribute("Title")!.Value)).DoesNotContain("Hidden Reg App");
      }

      await Matrix(
        v1_1: Assertion,
        v2: Assertion,
        v2_1: Assertion,
        user: UserInformation.AnonymousUser,
        managedResourceService: new StubManagementService()
      );
    }
    finally {
      DeleteRegistryApp(collectionName, appName);
    }
  }

}

// ----------------------------------------------------------------------------
// Tests and test helpers related to the ConstructIconElements
// method in WorkspaceBuilder.cs
// ----------------------------------------------------------------------------

public partial class WorkspaceBuilderTests {
  /// <summary>
  /// Writes an empty PNG image with the specified dimensions to disk.
  /// </summary>
  private static void WritePng(string path, int width, int height) {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var bmp = new System.Drawing.Bitmap(width, height);
    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
  }

  /// <summary>
  /// Creates in-memory empty PNG bytes for an image with the specified dimensions.
  /// </summary>
  private static byte[] PngBytes(int width, int height) {
    using var bmp = new System.Drawing.Bitmap(width, height);
    using var ms = new MemoryStream();
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    return ms.ToArray();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> returns an empty
  /// string when the user is null.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_NullUser_ReturnsEmptyString() {
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");

    var result = builder.ConstructIconElements(null, "some/path", WorkspaceBuilder.IconElementsMode.Icon);

    await Assert.That(result).IsEqualTo("");
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> returns an empty
  /// string when the specified icon is missing and <c>skipMissing</c> is <c>true</c>.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_SkipMissingTrue_ReturnsEmpty() {
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");

    var result = builder.ConstructIconElements(s_testUser, "nonexistent-icon", WorkspaceBuilder.IconElementsMode.Icon, skipMissing: true);

    await Assert.That(result).IsEqualTo("");
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> returns default
  /// icon elements when the specified icon is missing and <c>skipMissing</c> is <c>false</c>.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_SkipMissingFalse_FallsBackToDefaultIcon() {
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");

    var result = builder.ConstructIconElements(s_testUser, "nonexistent-icon", WorkspaceBuilder.IconElementsMode.Icon, skipMissing: false);

    await Assert.That(result.Contains("<IconRaw")).IsTrue();
    await Assert.That(result.Contains("FileType=\"Ico\"")).IsTrue();
    await Assert.That(result.Contains("defaulticon")).IsTrue();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> returns the
  /// provided or default image inside a PC frame when in wallpaper mode.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_WallpaperMode_UsesDefaultwallpaperAndFramePc() {
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");

    var result = builder.ConstructIconElements(
      s_testUser,
      "nonexistent-icon",
      WorkspaceBuilder.IconElementsMode.Wallpaper,
      "resource://static/lib/assets/wallpaper.png",
      skipMissing: false
    );

    await Assert.That(result.Contains("frame=pc")).IsTrue();
    await Assert.That(result.Contains("defaultwallpaper")).IsTrue();
  }

  /// <summary>
  /// Verifies that when a custom IIS base path is provided to the WorkspaceBuilder,
  /// the <cref name="WorkspaceBuilder.ConstructIconElements"/> method includes the
  /// IIS base path in front of the constructed icon URLs.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_CustomIisBase_AppearsInIconUrl() {
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com", iisBase: "/myapp/");

    var result = builder.ConstructIconElements(s_testUser, "nonexistent-icon", WorkspaceBuilder.IconElementsMode.Icon, skipMissing: false);

    await Assert.That(result.Contains("/myapp/api/")).IsTrue();
  }


  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> for a square file
  /// icon uses the real path and omits sizes larger than the provided icon size.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_SquareFileIcon_UsesRealPathAndOmitsLargerSizes() {
    // a 64x64 icon should not ever return Icon100 or Icon256
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "App64.png"), 64, 64);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "icons-test/App64", WorkspaceBuilder.IconElementsMode.Icon);

    await Assert.That(result.Contains("icons-test/App64?format=ico")).IsTrue();
    await Assert.That(result.Contains("<Icon16")).IsTrue();
    await Assert.That(result.Contains("<Icon32")).IsTrue();
    await Assert.That(result.Contains("<Icon48")).IsTrue();
    await Assert.That(result.Contains("<Icon64")).IsTrue();
    await Assert.That(result.Contains("<Icon100")).IsFalse();
    await Assert.That(result.Contains("<Icon256")).IsFalse();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> for a large square file
  /// icon includes all sizes up to 256x256.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_LargeSquareFileIcon_IncludesAllSizes() {
    // a 256x256 icon should return all sizes since it is as large as or larger than all of them
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "App256.png"), 256, 256);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "icons-test/App256", WorkspaceBuilder.IconElementsMode.Icon);

    await Assert.That(result.Contains("<Icon16")).IsTrue();
    await Assert.That(result.Contains("<Icon32")).IsTrue();
    await Assert.That(result.Contains("<Icon48")).IsTrue();
    await Assert.That(result.Contains("<Icon64")).IsTrue();
    await Assert.That(result.Contains("<Icon100")).IsTrue();
    await Assert.That(result.Contains("<Icon256")).IsTrue();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> for a tiny square file
  /// icon smaller than 16x16 only includes the raw icon element and omits all scaled sizes.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_TinySquareFileIcon_OmitsAllScaledSizes() {
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "App8.png"), 8, 8);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "icons-test/App8", WorkspaceBuilder.IconElementsMode.Icon);

    await Assert.That(result.Contains("<IconRaw")).IsTrue();
    await Assert.That(result.Contains("<Icon16")).IsFalse();
    await Assert.That(result.Contains("<Icon32")).IsFalse();
    await Assert.That(result.Contains("<Icon48")).IsFalse();
    await Assert.That(result.Contains("<Icon64")).IsFalse();
    await Assert.That(result.Contains("<Icon100")).IsFalse();
    await Assert.That(result.Contains("<Icon256")).IsFalse();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> in wallpaper mode
  /// accepts a non-square image and includes the frame=pc query parameter in the URLs,
  /// and that it omits sizes larger than the provided wallpaper size.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_WallpaperMode_NonSquareImageIsAllowedAndFramed() {
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "Wide.png"), 200, 100);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "icons-test/Wide", WorkspaceBuilder.IconElementsMode.Wallpaper);

    await Assert.That(result.Contains("icons-test/Wide")).IsTrue();
    await Assert.That(result.Contains("frame=pc")).IsTrue();
    await Assert.That(result.Contains("<IconRaw")).IsTrue();
    await Assert.That(result.Contains("<Icon16")).IsTrue();
    await Assert.That(result.Contains("<Icon32")).IsTrue();
    await Assert.That(result.Contains("<Icon48")).IsTrue();
    await Assert.That(result.Contains("<Icon64")).IsTrue();
    await Assert.That(result.Contains("<Icon100")).IsTrue();
    await Assert.That(result.Contains("<Icon256")).IsFalse();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> can read icons
  /// from a .resource managed resource file.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_ManagedResourceIconPath_ReadsDimensionsFromResourceFile() {
    var managedDir = Constants.ManagedResourcesFolderPath;
    Directory.CreateDirectory(managedDir);

    // create a managed resource
    var resourcePath = Path.Combine(managedDir, "IconApp.resource");
    var managedResource = new Management.ManagedFileResource(
      resourcePath,
      "Icon App",
      "full address:s:icon.example.com\r\n",
      iconPath: null,
      iconIndex: null,
      includeInWorkspace: true,
      virtualFolders: null,
      securityDescriptor: null
    );
    managedResource.WriteToFile();

    // write an icon to the managed resource
    using (var iconStream = new MemoryStream(PngBytes(48, 48))) {
      managedResource.WriteImage(iconStream, "resource.png", Management.ManagedFileResource.ImageTheme.Light);
    }

    // construct icon elements
    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "managed-resources/IconApp", WorkspaceBuilder.IconElementsMode.Icon);

    await Assert.That(result.Contains("managed-resources/IconApp?format=ico")).IsTrue();
    await Assert.That(result.Contains("<Icon48")).IsTrue();
    await Assert.That(result.Contains("<Icon64")).IsFalse();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> in icon mode falls
  /// back to the default icon when the specified icon is is not square.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_NonSquareIconInIconMode_FallsBackToDefaultWithoutThrowing() {
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "NonSquare.png"), 200, 100);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(UserInformation.AnonymousUser, "icons-test/NonSquare", WorkspaceBuilder.IconElementsMode.Icon);

    // expect the non-square icon to be rejected and replaced with the default icon.
    await Assert.That(result.Contains("<IconRaw")).IsTrue();
    await Assert.That(result.Contains("defaulticon")).IsTrue();
  }

  /// <summary>
  /// Verifies that <cref name="WorkspaceBuilder.ConstructIconElements"/> in icon mode falls
  /// back to the provided default icon when the specified icon is is not square.
  /// </summary>
  [Test]
  public async Task ConstructIconElements_NonSquareIconInIconMode_WithRealDefaultIcon_FallsBackToDefault() {
    WritePng(Path.Combine(Constants.AppDataFolderPath, "icons-test", "NonSquare2.png"), 200, 100);
    var defaultIconPath = Path.Combine(Constants.AppDataFolderPath, "icons-test", "RealDefault.png");
    WritePng(defaultIconPath, 32, 32);

    var builder = new WorkspaceBuilder(WorkspaceBuilder.SchemaVersion.v1_1, s_testUser, "example.com");
    var result = builder.ConstructIconElements(
      UserInformation.AnonymousUser,
      "icons-test/NonSquare2",
      WorkspaceBuilder.IconElementsMode.Icon,
      "icons-test/RealDefault.png"
    );

    Console.WriteLine(result);

    await Assert.That(result.Contains("icons-test/RealDefault.png?format=ico")).IsTrue();
    await Assert.That(result.Contains("<Icon32")).IsTrue();
    await Assert.That(result.Contains("<Icon48")).IsFalse();
  }
}
