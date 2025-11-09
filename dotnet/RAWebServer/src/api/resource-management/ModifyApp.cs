using System;
using System.IO;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// A version of <see cref="ManagedResource"/>  where all fields are optional/nullable.
    /// 
    /// Fields are only populated here if they are provided in the request body.
    /// which usually only occurs when the field needs to be updated.
    /// </summary>
    public class PartialManagedResource {
      public string Identifier { get; set; }
      public string Name { get; set; }
      public string IconPath { get; set; }
      public int? IconIndex { get; set; }
      public bool? IncludeInWorkspace { get; set; }
      public PartialRemoteAppProperties RemoteAppProperties { get; set; }
      public string RdpFileString { get; set; }
      public SecurityDescriptionDTO SecurityDescription { get; set; }
    }

    /// <summary>
    /// A version of <see cref="RemoteAppProperties"/> where all fields are optional/nullable.
    /// </summary>
    public class PartialRemoteAppProperties {
      public string ApplicationPath { get; set; }
      public string CommandLine { get; set; }
      public RemoteAppProperties.CommandLineMode? CommandLineOption { get; set; }
      public RemoteAppProperties.FileTypeAssociationCollection FileTypeAssociations { get; set; }
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
    [HttpPatch]
    [Route("registered/{*identifier}")]
    [RequireLocalAdministrator]
    public IHttpActionResult ModifyApp(string identifier, [FromBody] PartialManagedResource app) {
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;
      var remoteAppsUtil = new SystemRemoteApps(collectionName);

      if (app == null) {
        return BadRequest("Missing or invalid request body.");
      }

      // check if the app is already registered
      var resources = GetPopulatedManagedResources();
      var registeredApp = resources.GetByIdentifier(identifier);
      var alreadyExists = registeredApp != null;
      if (!alreadyExists) {
        return NotFound();
      }

      // check whether we need to move the app to a different registry key
      var isRenaming = !string.IsNullOrEmpty(app.Identifier) && !string.Equals(app.Identifier, identifier, StringComparison.OrdinalIgnoreCase);
      if (isRenaming) {
        // check if the new name is already taken
        var newNameAlreadyExists = resources.GetByIdentifier(app.Identifier) != null;
        if (newNameAlreadyExists) {
          return BadRequest("A RemoteApp with the new name (registry key) already exists.");
        }
      }

      // check whether the resource is a RemoteApp or a Desktop
      var isRemoteApp = registeredApp.RemoteAppProperties != null;


      // update the registered app

      if (registeredApp.Source == ManagedResourceSource.File) {
        var updatedApp = new ManagedFileResource(
          rootedFilePath: Path.Combine(Constants.ManagedResourcesFolderPath, identifier),
          name: app.Name ?? registeredApp.Name,
          rdpFileString: app.RdpFileString ?? registeredApp.RdpFileString,
          iconPath: app.IconPath ?? registeredApp.IconPath,
          iconIndex: app.IconIndex ?? registeredApp.IconIndex,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          securityDescriptor: app.SecurityDescription != null ? app.SecurityDescription.ToRawSecurityDescriptor() : registeredApp.SecurityDescriptor
        );

        // if a RemoteApp, we also need to update the RemoteApp properties
        if (isRemoteApp) {
          if (app.RemoteAppProperties == null) {
            app.RemoteAppProperties = new PartialRemoteAppProperties();
          }
          updatedApp.RemoteAppProperties = new RemoteAppProperties(
            applicationPath: app.RemoteAppProperties.ApplicationPath ?? registeredApp.RemoteAppProperties.ApplicationPath,
            commandLine: app.RemoteAppProperties.CommandLine ?? registeredApp.RemoteAppProperties.CommandLine,
            commandLineOption: app.RemoteAppProperties.CommandLineOption ?? registeredApp.RemoteAppProperties.CommandLineOption,
            fileTypeAssociations: app.RemoteAppProperties.FileTypeAssociations ?? registeredApp.RemoteAppProperties.FileTypeAssociations
          );
        }

        // write the updated app to file
        updatedApp.WriteToFile();

        // if renaming, delete the old file
        if (isRenaming) {
          (registeredApp as ManagedFileResource).Delete();
        }

        return Ok(GetPopulatedManagedResources().GetByIdentifier(updatedApp.Identifier));
      }

      try {
        // construct updated app
        if (app.RemoteAppProperties == null) {
          app.RemoteAppProperties = new PartialRemoteAppProperties();
        }
        var updatedApp = new SystemRemoteApps.SystemRemoteApp(
          key: app.Identifier ?? identifier,
          collectionName: collectionName,
          name: app.Name ?? registeredApp.Name,
          path: app.RemoteAppProperties.ApplicationPath ?? registeredApp.RemoteAppProperties.ApplicationPath,
          iconPath: app.IconPath ?? registeredApp.IconPath,
          iconIndex: app.IconIndex ?? registeredApp.IconIndex,
          commandLine: app.RemoteAppProperties.CommandLine ?? registeredApp.RemoteAppProperties.CommandLine,
          commandLineOption: app.RemoteAppProperties.CommandLineOption ?? registeredApp.RemoteAppProperties.CommandLineOption,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          fileTypeAssociations: app.RemoteAppProperties.FileTypeAssociations ?? registeredApp.RemoteAppProperties.FileTypeAssociations,
          securityDescription: app.SecurityDescription ?? registeredApp.SecurityDescription
        ) {
          RdpFileString = app.RdpFileString
        };

        try {
          SystemRemoteAppsClient.Proxy.WriteRemoteAppToRegistry(updatedApp);
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }

        // if renaming, delete the old registry key
        if (isRenaming) {
          try {
            SystemRemoteAppsClient.Proxy.DeleteRemoteAppFromRegistry(registeredApp as SystemRemoteApps.SystemRemoteApp);
          }
          catch (EndpointNotFoundException) {
            return InternalServerError(new Exception("The RAWeb Management Service is not running."));
          }
        }

        return Ok(GetPopulatedManagedResources().GetByIdentifier(updatedApp.Identifier));
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
