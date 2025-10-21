using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// A version of SystemRemoteApps.SystemRemoteApp where all fields are optional/nullable.
    /// </summary>
    public class PartialSystemRemoteApp {
      public string Key { get; set; }
      public string Name { get; set; }
      public string Path { get; set; }
      public string VPath { get; set; }
      public string IconPath { get; set; }
      public int? IconIndex { get; set; }
      public string CommandLine { get; set; }
      public SystemRemoteApps.SystemRemoteApp.CommandLineMode? CommandLineOption { get; set; }
      public bool? IncludeInWorkspace { get; set; }
      public SystemRemoteApps.FileTypeAssociations FileTypeAssociations { get; set; }
      public System.Security.AccessControl.RawSecurityDescriptor SecurityDescriptor { get; set; }
    }

    /// <summary>
    /// Modifies an existing RemoteApp application in the registry.
    /// <br /><br />
    /// Only the provided fields will be updated; fields that are null will be left unchanged.
    /// <br /><br />
    /// Specifying a different value in `PartialSystemRemoteApp.Key` will cause the application
    /// to be moved to a different registry key.
    /// </summary>
    /// <param name="key">The key for the RemoteApp in the registry</param>
    /// <returns></returns>
    [HttpPatch]
    [Route("registered/{*key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult ModifyApp(string key, [FromBody] PartialSystemRemoteApp app) {
      var remoteAppsUtil = new SystemRemoteApps();

      if (app == null) {
        return BadRequest("Missing or invalid request body.");
      }

      // check if the app is already registered
      var registeredApp = remoteAppsUtil.GetRegistedApp(key);
      var alreadyExists = registeredApp != null;
      if (!alreadyExists) {
        return NotFound();
      }

      // check whether we need to move the app to a different registry key
      var isRenaming = !string.IsNullOrEmpty(app.Key) && !string.Equals(app.Key, key, StringComparison.OrdinalIgnoreCase);
      if (isRenaming) {
        // check if the new name is already taken
        var newNameAlreadyExists = remoteAppsUtil.GetRegistedApp(app.Key) != null;
        if (newNameAlreadyExists) {
          return BadRequest("A RemoteApp with the new name (registry key) already exists.");
        }
      }

      // update the registered app
      try {
        // construct updated app
        var updatedApp = new SystemRemoteApps.SystemRemoteApp(
          key: app.Key ?? key,
          name: app.Name ?? registeredApp.Name,
          path: app.Path ?? registeredApp.Path,
          vPath: app.VPath ?? registeredApp.VPath,
          iconPath: app.IconPath ?? registeredApp.IconPath,
          iconIndex: app.IconIndex ?? registeredApp.IconIndex,
          commandLine: app.CommandLine ?? registeredApp.CommandLine,
          commandLineOption: app.CommandLineOption ?? registeredApp.CommandLineOption,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          fileTypeAssociations: app.FileTypeAssociations ?? registeredApp.FileTypeAssociations,
          securityDescriptor: app.SecurityDescriptor ?? registeredApp.SecurityDescriptor
        );

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

        return Ok(remoteAppsUtil.GetRegistedApp(updatedApp.Key));
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
