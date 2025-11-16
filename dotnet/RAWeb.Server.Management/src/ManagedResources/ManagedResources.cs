using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Management;

#if NET462
/// <summary>
/// WCF service contract for managing RemoteApp programs and desktops in the system registry.
/// <br /><br />
/// This contract is implemented by RAWeb.Server.Management.ServiceHost.SystemDesktopService.
/// The service is itended to run with elevated/administrative privileges,
/// allowing it to read and write RemoteApp and desktop definitions in the system registry.
/// All other processes (such as RAWeb web server) should access RemoteApp and desktop management
/// functionality via this service to ensure that they have the necessary privileges. Therefore,
/// all other processes should use a WCF client proxy to call this service instead of directly accessing
/// the RAWeb.Server.Management.SystemRemoteApps and RAWeb.Server.Management.SystemDesktop classes
/// for elevated operations. Additionally, these processes should NOT run with elevated privileges
/// themselves to minimize security risks.
/// </summary>
[ServiceContract]
public interface IManagedResourceService {
  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.EnsureRegistryPathExists</c>.
  /// </summary>
  [OperationContract]
  void InitializeRegistryPaths(string? collectionName = null);

  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.SystemRemoteApp.WriteToRegistry</c>.
  /// </summary>
  [OperationContract]
  void WriteRemoteAppToRegistry(SystemRemoteApps.SystemRemoteApp app);

  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.SystemRemoteApp.DeleteFromRegistry</c>.
  /// </summary>
  [OperationContract]
  void DeleteRemoteAppFromRegistry(SystemRemoteApps.SystemRemoteApp app);

  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.GetAllRegisteredApps</c>
  /// with restorePackagedAppIconPaths set to true.
  /// </summary>
  [OperationContract]
  void RestorePackagedAppIconPaths();

  /// <summary>
  /// Service implementation of <c>InstalledApps.FromStartMenu</c> and <c>InstalledApps.FromAppPackages</c>.
  /// </summary>
  [OperationContract]
  InstalledApps ListInstalledApps(string? userSid = null);

  /// <summary>
  /// Service implementation of <c>SystemDesktop.EnsureRegistryPathExists</c>.
  /// </summary>
  [OperationContract]
  void InitializeDesktopRegistryPaths(string collectionName);

  /// <summary>
  /// Service implementation of <c>SystemDesktop.WriteToRegistry</c>.
  /// </summary>
  [OperationContract]
  void WriteDesktopToRegistry(SystemDesktop desktop);

  /// <summary>
  /// Service implementation of <c>SystemDesktop.DeleteFromRegistry</c>.
  /// </summary>
  [OperationContract]
  void DeleteDesktopFromRegistry(SystemDesktop desktop);

  /// <summary>
  /// Service implementation of <c>SystemDesktop.GetWallpaperStream</c>.
  /// </summary>
  [OperationContract]
  [FaultContract(typeof(ManageResourceServiceFault))]
  Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid);
}

/// <summary>
/// A FaultException for managed resource service errors. This is the only type of exception that
/// the managed resource service methods can throw that will be properly transmitted to the client.
/// </summary>
/// <param name="name"></param>
/// <param name="message"></param>
public class ManagedResourceFaultException(string name, string message) : FaultException<ManageResourceServiceFault>(new ManageResourceServiceFault(name, message), message) {
  public string Name => Detail.Name;
  public override string Message => Detail.Message;
  public override string ToString() {
    return $"{Detail.Name}: {Detail.Message}";
  }

  public static ManagedResourceFaultException FromException(Exception ex) {
    return new ManagedResourceFaultException(ex.GetType().Name, ex.Message);
  }
}

[DataContract]
public class ManageResourceServiceFault(string name, string message) {
  [DataMember]
  public string Name { get; set; } = name;

  [DataMember]
  public string Message { get; set; } = message;
}
#endif

[DataContract]
[JsonConverter(typeof(ManagedResourceDeserializer))]
public abstract class ManagedResource(ManagedResourceSource source, string identifier, string name, string? iconPath) {
  /// <summary>
  /// The source type for this managed resource. Use this to determine where
  /// the resource is stored, which affects the classes used to manage it.
  /// </summary>
  [DataMember] public ManagedResourceSource Source { get; private set; } = source;
  /// <summary>
  /// The unique identifier for this managed resource. When stored in the
  /// registry, this is the key name. When stored as a file, this is the
  /// file name without extension.
  /// </summary>
  [DataMember] public string Identifier { get; set; } = identifier;
  /// <summary>
  /// The name of the managed resource. This is the display name
  /// shown to users.
  /// </summary>
  [DataMember] public string Name { get; set; } = name;

  /// <summary>
  /// The path to the icon file for this managed resource.
  /// <br /><br />
  /// Depending on the value of <see cref="Source"/>, this path may be
  /// absolute or relative.
  /// </summary>
  [DataMember] public string? IconPath { get; set; } = iconPath;
  /// <summary>
  /// The index of the icon within the icon file for this managed resource.
  /// <br /><br />
  /// This is typically 0 for most resources. For resources that use DLL,
  /// EXE, or ICO files with multiple icons, this index specifies which icon to use.
  /// </summary>
  [DataMember] public int IconIndex { get; set; } = 0;

  /// <summary>
  /// Whether this managed resource should appear in the workspace feed.
  /// When false, the resource is hidden from users, but it may still be
  /// assessible via direct download links or other means.
  /// </summary>
  [DataMember] public bool IncludeInWorkspace { get; set; } = false;

  /// <summary>
  /// If this managed resource is a RemoteApp, this property contains
  /// the RemoteApp-specific properties. Otherwise, this property is null.
  /// <br /><br />
  /// To check whether this resource is a RemoteApp, check whether
  /// this property is non-null:
  /// <code>var isRemoteApp = myManagedResource.RemoteAppProperties is not null;</code>
  /// </summary>
  [DataMember] public RemoteAppProperties? RemoteAppProperties { get; set; }

  /// <summary>
  /// The RDP file string for this managed resource, if applicable.
  /// <br /><br />
  /// When the value of <see cref="Source"/> is <see cref="ManagedResourceSource.File"/>,
  /// this property contains the contents of the .rdp file stored on disk.
  /// <br /><br />
  /// When the value of <see cref="Source"/> is <see cref="ManagedResourceSource.TSAppAllowList"/>
  /// this property will be ignored since there is no mechanism to store custom RDP settings
  /// in the TSAppAllowList registry entries.
  /// <br /><br />
  /// When the value of <see cref="Source"/> is <see cref="ManagedResourceSource.CentralPublishedResourcesApp"/>,
  /// this property contains the RDP settings stored in the registry key for the resource in the collection.
  /// </summary>
  [DataMember] public string? RdpFileString { get; set; }

  /// <summary>
  /// The security descriptor for this managed resource. This security
  /// descriptor defines access permissions based on SID presence
  /// with ReadData rights.
  /// <br /><br />
  /// This property is ignored for serialization; use the
  /// <see cref="SecurityDescription"/> property instead.
  /// </summary>
  public RawSecurityDescriptor? SecurityDescriptor { get; set; }
  /// <summary>
  /// The serialized security description for this managed resource.
  /// The serialized form only inclucdes the list of SIDs with ReadData access
  /// allowed or denied.
  /// <br /><br />
  /// Setting this property will update the non-serializable
  /// <see cref="SecurityDescriptor"/> property accordingly.
  /// </summary>
  [DataMember]
  public SecurityDescriptionDTO? SecurityDescription {
    get {
      if (SecurityDescriptor is null) {
        return null;
      }

      var allowed = SecurityDescriptorExtensions
          .GetExplicitlyAllowedSids(SecurityDescriptor, FileSystemRights.ReadData)
          .Select(sid => sid.Value)
          .ToList();

      var denied = SecurityDescriptorExtensions
          .GetExplicitlyDeniedSids(SecurityDescriptor, FileSystemRights.ReadData)
          .Select(sid => sid.Value)
          .ToList();

      return new SecurityDescriptionDTO(allowed, denied);
    }
    set {
      if (value is null) {
        SecurityDescriptor = null;
        return;
      }

      // set the non-serializable security descriptor property
      SecurityDescriptor = value.ToRawSecurityDescriptor();
    }
  }


  /// <summary>
  /// Sets the source type for this managed resource. You probably shouldn't
  /// need to call this directly.Most classes that ineherit from
  /// ManagedResource will set this automatically as needed.
  /// </summary>
  /// <param name="source"></param>
  public void SetSource(ManagedResourceSource source) {
    Source = source;
  }

  /// <summary>
  /// Generates the contents of an RDP file for this RemoteApp.
  /// </summary>
  /// <param name="fullAddressOverride">If specified, the full address value in the RDP file will be replaced with this address.</param>
  /// <returns></returns>
  public abstract StringBuilder ToRdpFileStringBuilder(string? fullAddressOverride);

  /// <summary>
  /// Gets the timestamp for when this managed resource was last modified (in UTC).
  /// <br /><br />
  /// The meaning of "last modified" may vary depending on the source type.
  /// For file-based resources, this typically means the last write time
  /// of the contents of the resource file (not the actual file). For registry-based
  /// resources, this typically means the last write time of the registry key.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="FileNotFoundException">For resource files if the file was not found</exception>
  /// <exception cref="Exception"></exception>
  public abstract DateTime GetLastWriteTimeUtc();

  /// <summary>
  /// Gets the timestamp for when this managed resource was last modified (in UTC).
  /// See <see cref="GetLastWriteTimeUtc"/> for details.
  /// </summary>
  /// <returns></returns>
  public DateTime GetLastWriteTimeUtcOrDefault() {
    try {
      return GetLastWriteTimeUtc();
    }
    catch {
      return DateTime.MinValue;
    }
  }
}

/// <summary>
/// A collection of managed resources.
/// <br /><br />
/// This class loads all managed resources from their respective
/// storage locations (registry, files, etc.) upon initialization.
/// </summary>
[CollectionDataContract]
public class ManagedResources : Collection<ManagedResource> {
  public ManagedResources() { }
  public ManagedResources(IList<ManagedResource> resources) : base(resources) { }
  public ManagedResources(IEnumerable<ManagedResource> resources) : base([.. resources]) { }

  /// <summary>
  /// Populates the managed resources collection from the specified
  /// registry collection name and resource files directory.
  /// </summary>
  /// <param name="collectionName"></param>
  /// <param name="resourceFilesDirectory"></param>
  /// <returns></returns>
  public ManagedResources Populate(string? collectionName, string? resourceFilesDirectory = null, bool? restorePackagedAppIconPaths = false) {
    Clear();

    // load all registry RemoteApps for the specified collection
    var remoteAppsUtil = new SystemRemoteApps(collectionName);
    var systemRemoteApps = remoteAppsUtil.GetAllRegisteredApps(restorePackagedAppIconPaths);
    foreach (var app in systemRemoteApps) {
      Add(app);
    }

    // load file-based managed resources
    var fileSystemRemoteApps = resourceFilesDirectory is not null ? ManagedFileResources.FromDirectory(resourceFilesDirectory) : [];
    foreach (var app in fileSystemRemoteApps) {
      Add(app);
    }

    // add the system desktop
    if (collectionName is not null) {
      var systemDesktop = SystemDesktop.FromRegistry(collectionName, collectionName);
      if (systemDesktop is null) {
        systemDesktop = new SystemDesktop(collectionName, collectionName);
        systemDesktop.WriteToRegistry();
      }
      if (systemDesktop is not null) {
        Add(systemDesktop);
      }
    }

    return this;
  }

  /// <summary>
  /// Gets a managed resource by its identifier.
  /// <br /><br />
  /// Before calling this method, populate the collection using
  /// the <see cref="Populate"/> method.
  /// </summary>
  /// <param name="identifier"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="InvalidOperationException"></exception>
  /// <exception cref="ManagedResourceNotFoundException"></exception>
  public ManagedResource GetByIdentifier(string identifier) {
    if (string.IsNullOrWhiteSpace(identifier)) {
      throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
    }

    if (Count == 0) {
      throw new InvalidOperationException("The managed resources collection is empty.");
    }

    try {
      return this.First(resource => string.Equals(resource.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
    }
    catch (InvalidOperationException) {
      throw new ManagedResourceNotFoundException(identifier);
    }
  }

  /// <summary>
  /// Tries to get a managed resource by its identifier.
  /// <br /><br />
  /// Before calling this method, populate the collection using
  /// the <see cref="Populate"/> method.
  /// </summary>
  /// <param name="identifier"></param>
  /// <returns></returns>
  public ManagedResource? TryGetByIdentifier(string identifier) {
    try {
      return GetByIdentifier(identifier);
    }
    catch (ManagedResourceNotFoundException) {
      return null;
    }
  }

  public class ManagedResourceNotFoundException(string identifier) : Exception($"No managed resource with identifier '{identifier}' was found.") { }
}


/// <summary>
/// The source type for a managed resource. 
/// </summary>
public enum ManagedResourceSource {
  /// <summary>
  /// A .resource file stored on disk. It contains the RDP file and associated metadata files.
  /// </summary>
  File,
  /// <summary>
  /// A resource stored in the TSAppAllowList registry applications list.
  /// </summary>
  TSAppAllowList,
  /// <summary>
  /// A resource published via centralized publishing to a specific collection in the registry AND included in the TSAppAllowList in the registry.
  /// </summary>
  CentralPublishedResourcesApp,
  /// <summary>
  /// A desktop published via centralized publishing to a specific collection in the registry.
  /// </summary>
  CentralPublishedResourcesDesktop
}

/// <summary>
/// The security description data transfer object (DTO) for a managed resource.
/// <br /><br />
/// This class should not be used for checking access permissions.
/// Instead, use <see cref="SecurityDescriptorExtensions.GetAllowedSids"/>.
/// </summary>
/// <param name="readAccessAllowedSids"></param>
/// <param name="readAccessDeniedSids"></param>
[DataContract]
public class SecurityDescriptionDTO(List<string>? readAccessAllowedSids = null, List<string>? readAccessDeniedSids = null) {
  /// <summary>
  /// The list of SIDs that are explicitly allowed ReadData access.
  /// </summary>
  [DataMember] public List<string> ReadAccessAllowedSids { get; set; } = readAccessAllowedSids ?? [];
  /// <summary>
  /// The list of SIDs that are explicitly denied ReadData access.
  /// </summary>
  [DataMember] public List<string> ReadAccessDeniedSids { get; set; } = readAccessDeniedSids ?? [];

  public RawSecurityDescriptor? ToRawSecurityDescriptor() {
    return SecurityTransformers.SidRightsToRawSecurityDescriptor(
      allowedSids: ReadAccessAllowedSids.ConvertAll(sid => new Tuple<string, FileSystemRights?>(sid, FileSystemRights.ReadData)),
      deniedSids: ReadAccessDeniedSids.ConvertAll(sid => new Tuple<string, FileSystemRights?>(sid, FileSystemRights.ReadData))
    );
  }
}

[DataContract]
public class RemoteAppProperties(string applicationPath, RemoteAppProperties.CommandLineMode commandLineOption, string? commandLine = null, RemoteAppProperties.FileTypeAssociationCollection? fileTypeAssociations = null) {
  /// <summary>
  /// The full path to the application executable for this RemoteApp or the "||registryKeyName" value.
  /// <br /><br />
  /// If the application is a packaged application (such as a UWP app), this path
  /// should be C:\Windows\explorer.exe and <see cref="CommandLine"/> should include the
  /// appropriate shell:AppsFolder\__full_package_name__!__app_id__ protocol to launch the application.
  /// </summary>
  [DataMember] public string ApplicationPath { get; init; } = applicationPath;

  /// <summary>
  /// The command line mode/option for this RemoteApp.
  /// </summary>
  [DataMember] public CommandLineMode CommandLineOption { get; init; } = commandLineOption;
  /// <summary>
  /// The command line arguments to be included when launching this RemoteApp.
  /// <br /><br />
  /// This is only used if <see cref="CommandLineOption"/> is not set to Disabled.
  /// <br /><br />
  /// This is especially useful for Progressive Web Apps (PWAs) that require URL arguments
  /// for the browser to open the correct web application.
  /// </summary>
  [DataMember] public string? CommandLine { get; init; } = commandLine;

  /// <summary>
  /// The file type associations for this RemoteApp.
  /// </summary>
  [DataMember] public FileTypeAssociationCollection FileTypeAssociations { get; init; } = fileTypeAssociations ?? [];

  /// <summary>
  /// The command line mode for a RemoteApp.
  /// </summary>
  public enum CommandLineMode {
    /// <summary>
    /// The command line option is disabled; no command line will be passed.
    /// </summary>
    Disabled = 0,
    /// <summary>
    /// The command line option is optional; it may be passed if specified.
    /// </summary>
    Optional = 1,
    /// <summary>
    /// The command line option is enforced; it will always be passed.
    /// </summary>
    Enforced = 2
  }

  /// <summary>
  /// Represents a file type association for a RemoteApp.
  /// </summary>
  [DataContract]
  public class FileTypeAssociation(string extension, string iconPath, int iconIndex = 0) {
    /// <summary>
    /// The file extension for this association (including the leading dot).
    /// </summary>
    [DataMember] public string Extension { get; set; } = extension;
    /// <summary>
    /// The path to the icon file for this file type association.
    /// <br /><br />
    /// The path may be absolute or relative, depending on context.
    /// </summary>
    [DataMember] public string IconPath { get; set; } = iconPath;
    /// <summary>
    /// The index of the icon within the icon file for this file type association.
    /// <br /><br />
    /// This is typically 0 for most file type associations. For file type associations that use DLL,
    /// EXE, or ICO files with multiple icons, this index specifies which icon to use.
    /// </summary>
    [DataMember] public int IconIndex { get; set; } = iconIndex;
  }

  /// <summary>
  /// A collection of file type associations for a RemoteApp.
  /// </summary>
  [CollectionDataContract]
  public class FileTypeAssociationCollection : Collection<FileTypeAssociation> {
    public FileTypeAssociationCollection() {
    }
    public FileTypeAssociationCollection(IList<FileTypeAssociation> associations) : base(associations) {
    }
    public FileTypeAssociationCollection(IEnumerable<FileTypeAssociation> associations) : base([.. associations]) {
    }
  }
}

public class ManagedResourceDeserializer : JsonConverter {
  public static string RootedManagedResourcesPath { get; set; } = "";

  public override bool CanConvert(Type objectType) {
    return objectType == typeof(ManagedResource);
  }

  public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
    // load the JSON for the current object into a JObject
    var jsonObject = JObject.Load(reader);

    // determine the source type
    var sourceInteger = jsonObject["source"]?.Value<int>();
    if (sourceInteger is null) {
      throw new JsonSerializationException("The 'source' property is missing or invalid.");
    }
    var source = (ManagedResourceSource)sourceInteger;

    // delegate to the appropriate subclass based on the source type
    if (source == ManagedResourceSource.File) {
      return ManagedFileResource.FromJSON(jsonObject, RootedManagedResourcesPath, serializer);
    }
    if (source == ManagedResourceSource.TSAppAllowList || source == ManagedResourceSource.CentralPublishedResourcesApp) {
      var app = SystemRemoteApps.SystemRemoteApp.FromJSON(jsonObject, serializer);
      return app;
    }
    if (source == ManagedResourceSource.CentralPublishedResourcesDesktop) {
      var desktop = SystemDesktop.FromJSON(jsonObject, serializer);
      return desktop;
    }

    throw new JsonSerializationException($"Unknown ManagedResource Source: {source}");
  }

  // let the default serialization handle writing
  public override bool CanWrite {
    get {
      return false;
    }
  }

  // not used because CanWrite = false
  public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
    throw new NotSupportedException();
  }
}
