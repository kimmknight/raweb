using Newtonsoft.Json;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ModifyAppEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPatch("/api/management/resources/registered/{*identifier}", Handle);
  }

  /// <summary>
  /// Modifies an existing managed RemoteApp or desktop.
  /// <br /><br />
  /// Only the provided fields will be updated; fields that are null will be left unchanged.
  /// <br /><br />
  /// Specifying a different value in `PartialManagedResource.Identifier` will cause the application
  /// to be moved to a different registry key or different file path.
  /// </summary>
  /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
  /// <returns></returns>
  private static async Task<IResult> Handle(string identifier, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var json = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var app = JsonConvert.DeserializeObject<PartialManagedResource>(json);
    if (app is null) {
      return Results.BadRequest("Missing or invalid request body.");
    }

    var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
    var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

    // check if the app is already registered
    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources();
    var registeredApp = resources.TryGetByIdentifier(identifier);
    if (registeredApp is null) {
      return Results.NotFound();
    }

    // check whether we need to move the app to a different registry key
    var isRenaming = !string.IsNullOrEmpty(app.Identifier) && !string.Equals(app.Identifier, identifier, StringComparison.OrdinalIgnoreCase);
    if (isRenaming && resources.TryGetByIdentifier(app.Identifier!) is not null) {
      // check if the new name is already taken
      var newNameAlreadyExists = resources.TryGetByIdentifier(app.Identifier!) is not null;
      if (newNameAlreadyExists) {
        return Results.BadRequest("A RemoteApp with the new name (registry key) already exists.");
      }
    }

    // check whether the resource is a RemoteApp or a Desktop
    var isRemoteApp = registeredApp.RemoteAppProperties is not null;

    // update the registered app

    try {
      if (registeredApp.Source == ManagedResourceSource.File) {
        var desintationPath = Path.Combine(Constants.ManagedResourcesFolderPath, app.Identifier ?? identifier);

        // if renaming, move the file before updating
        if (isRenaming) {
          (registeredApp as ManagedFileResource)!.MoveTo(desintationPath);
        }

        var updatedApp = new ManagedFileResource(
          rootedFilePath: desintationPath,
          name: app.Name ?? registeredApp.Name,
          rdpFileString: app.RdpFileString ?? registeredApp.RdpFileString ?? string.Empty,
          iconPath: app.IconPath ?? registeredApp.IconPath,
          iconIndex: app.IconIndex ?? registeredApp.IconIndex,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          securityDescriptor: app.SecurityDescription is not null ? app.SecurityDescription.ToRawSecurityDescriptor() : registeredApp.SecurityDescriptor,
          virtualFolders: app.VirtualFolders ?? registeredApp.VirtualFolders
        );

        // if a RemoteApp, we also need to update the RemoteApp properties
        if (isRemoteApp) {
          app.RemoteAppProperties ??= new PartialRemoteAppProperties();
          updatedApp.RemoteAppProperties = new RemoteAppProperties(
            applicationPath: app.RemoteAppProperties.ApplicationPath ?? registeredApp.RemoteAppProperties!.ApplicationPath,
            commandLine: app.RemoteAppProperties.CommandLine ?? registeredApp.RemoteAppProperties!.CommandLine,
            commandLineOption: app.RemoteAppProperties.CommandLineOption ?? registeredApp.RemoteAppProperties!.CommandLineOption,
            fileTypeAssociations: app.RemoteAppProperties.FileTypeAssociations ?? registeredApp.RemoteAppProperties!.FileTypeAssociations
          );
        }

        // if there are base64-encoded managed icons, update them
        if (app.ManagedIconLightBase64 is not null) {
          Stream? lightStream = app.ManagedIconLightBase64 == "" ? null : new MemoryStream(Convert.FromBase64String(app.ManagedIconLightBase64));
          updatedApp.WriteImage(lightStream, "resource.png", ManagedFileResource.ImageTheme.Light);
        }
        if (app.ManagedIconDarkBase64 is not null) {
          Stream? darkStream = app.ManagedIconDarkBase64 == "" ? null : new MemoryStream(Convert.FromBase64String(app.ManagedIconDarkBase64));
          updatedApp.WriteImage(darkStream, "resource.png", ManagedFileResource.ImageTheme.Dark);
        }

        // write the updated app to file
        updatedApp.WriteToFile();

        return Results.Content(JsonConvert.SerializeObject(
            GetRegisteredAppsEndpoint.GetPopulatedManagedResources().GetByIdentifier(updatedApp.Identifier)
        ), "application/json");
      }

      if (registeredApp.Source == ManagedResourceSource.CentralPublishedResourcesDesktop) {
        if (isRenaming) {
          return Results.BadRequest("Renaming desktops is not supported.");
        }

        if (collectionName is null) {
          return Results.BadRequest("Centralized publishing is not enabled for this RAWeb application.");
        }

        // construct updated desktop
        var updatedDesktop = new SystemDesktop(
          identifier: app.Identifier ?? identifier,
          collectionName: collectionName,
          desktopName: app.Name ?? registeredApp.Name,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          securityDescriptor: app.SecurityDescription is not null ? app.SecurityDescription.ToRawSecurityDescriptor() : registeredApp.SecurityDescriptor,
          rdpFileString: app.RdpFileString ?? registeredApp.RdpFileString,
          virtualFolders: app.VirtualFolders ?? registeredApp.VirtualFolders
        );

        try {
          ManagementServiceClient.Proxy.WriteDesktopToRegistry(updatedDesktop);
        }
        catch (UnauthorizedAccessException) {
          ManagementServiceClient.Proxy.InitializeDesktopRegistryPaths(collectionName);
          ManagementServiceClient.Proxy.WriteDesktopToRegistry(updatedDesktop);
        }
        catch (EndpointNotFoundException) {
          return Results.Problem("The RAWeb Management Service is not running.", statusCode: 500);
        }

        return Results.Content(JsonConvert.SerializeObject(
            GetRegisteredAppsEndpoint.GetPopulatedManagedResources().GetByIdentifier(updatedDesktop.Identifier)
        ), "application/json");
      }

      // construct updated registry RemoteApp
      app.RemoteAppProperties ??= new PartialRemoteAppProperties();
      var updatedRegistryApp = new SystemRemoteApps.SystemRemoteApp(
        key: app.Identifier ?? identifier,
        collectionName: collectionName,
        name: app.Name ?? registeredApp.Name,
        path: app.RemoteAppProperties.ApplicationPath ?? registeredApp.RemoteAppProperties!.ApplicationPath,
        iconPath: app.IconPath ?? registeredApp.IconPath ?? string.Empty,
        iconIndex: app.IconIndex ?? registeredApp.IconIndex,
        commandLine: app.RemoteAppProperties.CommandLine ?? registeredApp.RemoteAppProperties!.CommandLine,
        commandLineOption: app.RemoteAppProperties.CommandLineOption ?? registeredApp.RemoteAppProperties!.CommandLineOption,
        includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
        fileTypeAssociations: app.RemoteAppProperties.FileTypeAssociations ?? registeredApp.RemoteAppProperties!.FileTypeAssociations,
        securityDescription: app.SecurityDescription ?? registeredApp.SecurityDescription,
        virtualFolders: app.VirtualFolders ?? registeredApp.VirtualFolders
      ) {
        RdpFileString = app.RdpFileString
      };

      try {
        ManagementServiceClient.Proxy.WriteRemoteAppToRegistry(updatedRegistryApp);
      }
      catch (EndpointNotFoundException) {
        return Results.Problem("The RAWeb Management Service is not running.", statusCode: 500);
      }

      // if renaming, delete the old registry key
      if (isRenaming) {
        try {
          ManagementServiceClient.Proxy.DeleteRemoteAppFromRegistry(registeredApp as SystemRemoteApps.SystemRemoteApp ?? throw new InvalidOperationException());
        }
        catch (EndpointNotFoundException) {
          return Results.Problem("The RAWeb Management Service is not running.", statusCode: 500);
        }
      }

      return Results.Content(JsonConvert.SerializeObject(
          GetRegisteredAppsEndpoint.GetPopulatedManagedResources().GetByIdentifier(updatedRegistryApp.Identifier)
      ), "application/json");
    }
    catch (Exception ex) {
      return Results.Problem(ex.Message, statusCode: 500);
    }
  }
}

/// <summary>
/// A version of <see cref="ManagedResource"/> where all fields are optional/nullable.
///
/// Fields are only populated here if they are provided in the request body.
/// which usually only occurs when the field needs to be updated.
/// </summary>
public class PartialManagedResource {
  public string? Identifier { get; set; }
  public string? Name { get; set; }
  public string? IconPath { get; set; }
  public int? IconIndex { get; set; }
  public bool? IncludeInWorkspace { get; set; }
  public PartialRemoteAppProperties? RemoteAppProperties { get; set; }
  public string? RdpFileString { get; set; }
  public SecurityDescriptionDTO? SecurityDescription { get; set; }
  public string[]? VirtualFolders { get; set; }
  public string? ManagedIconLightBase64 { get; set; }
  public string? ManagedIconDarkBase64 { get; set; }
}

/// <summary>
/// A version of <see cref="RemoteAppProperties"/> where all fields are optional/nullable.
/// </summary>
public class PartialRemoteAppProperties {
  public string? ApplicationPath { get; set; }
  public string? CommandLine { get; set; }
  public RemoteAppProperties.CommandLineMode? CommandLineOption { get; set; }
  public RemoteAppProperties.FileTypeAssociationCollection? FileTypeAssociations { get; set; }
}
