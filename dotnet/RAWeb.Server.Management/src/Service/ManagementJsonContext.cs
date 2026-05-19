using System.Text.Json.Serialization;

namespace RAWeb.Server.Management;

[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(SecurityDescriptionDTO))]
[JsonSerializable(typeof(RemoteAppProperties))]
[JsonSerializable(typeof(RemoteAppProperties.FileTypeAssociation))]
[JsonSerializable(typeof(RemoteAppProperties.FileTypeAssociationCollection))]
[JsonSerializable(typeof(SystemRemoteApps.SystemRemoteApp))]
[JsonSerializable(typeof(SystemDesktop))]
[JsonSerializable(typeof(ManagedFileResource))]
[JsonSerializable(typeof(ManagedResource))]
[JsonSerializable(typeof(ManagedResources))]
[JsonSerializable(typeof(InstalledApp))]
[JsonSerializable(typeof(InstalledApp[]))]
public partial class ManagementJsonContext : JsonSerializerContext { }
