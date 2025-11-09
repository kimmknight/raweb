using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// A version of <see cref="SystemRemoteApps.SystemRemoteApp"/>  where all fields are optional/nullable.
    /// </summary>
    public class PartialSystemRemoteApp {
      // public ManagedResourceSource? Source { get; set; }
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
    /// Modifies an existing RemoteApp application in the registry.
    /// <br /><br />
    /// Only the provided fields will be updated; fields that are null will be left unchanged.
    /// <br /><br />
    /// Specifying a different value in `PartialSystemRemoteApp.Key` will cause the application
    /// to be moved to a different registry key.
    /// </summary>
    /// <param name="identifier">The key for the RemoteApp in the registry</param>
    /// <returns></returns>
    [HttpPatch]
    [Route("registered/{*identifier}")]
    [RequireLocalAdministrator]
    public IHttpActionResult ModifyApp(string identifier, [FromBody] PartialSystemRemoteApp app) {
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;
      var remoteAppsUtil = new SystemRemoteApps(collectionName);

      if (app == null) {
        return BadRequest("Missing or invalid request body.");
      }

      // check if the app is already registered
      var registeredApp = remoteAppsUtil.GetRegistedApp(identifier);
      var alreadyExists = registeredApp != null;
      if (!alreadyExists) {
        return NotFound();
      }

      // check whether we need to move the app to a different registry key
      var isRenaming = !string.IsNullOrEmpty(app.Identifier) && !string.Equals(app.Identifier, identifier, StringComparison.OrdinalIgnoreCase);
      if (isRenaming) {
        // check if the new name is already taken
        var newNameAlreadyExists = remoteAppsUtil.GetRegistedApp(app.Identifier) != null;
        if (newNameAlreadyExists) {
          return BadRequest("A RemoteApp with the new name (registry key) already exists.");
        }
      }

      // update the registered app
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
            SystemRemoteAppsClient.Proxy.DeleteRemoteAppFromRegistry(registeredApp);
          }
          catch (EndpointNotFoundException) {
            return InternalServerError(new Exception("The RAWeb Management Service is not running."));
          }
        }

        return Ok(remoteAppsUtil.GetRegistedApp(updatedApp.Identifier));
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
