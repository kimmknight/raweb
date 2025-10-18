using System;
using System.Web.Http;
using RAWebServer.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// A version of SystemRemoteApps.SystemRemoteApp where all fields are optional/nullable.
    /// </summary>
    public class PartialSystemRemoteApp {
      public string Name { get; set; }
      public string FullName { get; set; }
      public string Path { get; set; }
      public string VPath { get; set; }
      public string IconPath { get; set; }
      public int? IconIndex { get; set; }
      public string CommandLine { get; set; }
      public SystemRemoteApps.SystemRemoteApp.CommandLineMode? CommandLineOption { get; set; }
      public bool? IncludeInWorkspace { get; set; }
      public SystemRemoteApps.FileTypeAssociationCollection FileTypeAssociations { get; set; }
      public System.Security.AccessControl.RawSecurityDescriptor securityDescriptor { get; set; }
    }

    /// <summary>
    /// Modifies an existing RemoteApp application in the registry.
    /// <br /><br />
    /// Only the provided fields will be updated; fields that are null will be left unchanged.
    /// <br /><br />
    /// Specifying a different name in `PartialSystemRemoteApp.Name` will cause the application
    /// to be moved to a different registry key.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    [HttpPatch]
    [Route("registered/{*appName}")]
    [RequireLocalAdministrator]
    public IHttpActionResult ModifyApp(string appName, [FromBody] PartialSystemRemoteApp app) {
      var remoteAppsUtil = new SystemRemoteApps();


      // check if the app is already registered
      var registeredApp = remoteAppsUtil.GetRegistedApp(appName);
      var alreadyExists = registeredApp != null;
      if (!alreadyExists) {
        return NotFound();
      }

      // check whether we need to move the app to a different registry key
      var isRenaming = !string.IsNullOrEmpty(app.Name) && !string.Equals(app.Name, appName, StringComparison.OrdinalIgnoreCase);
      if (isRenaming) {
        // check if the new name is already taken
        var newNameAlreadyExists = remoteAppsUtil.GetRegistedApp(app.Name) != null;
        if (newNameAlreadyExists) {
          return BadRequest("A RemoteApp with the new name (registry key) already exists.");
        }
      }

      // update the registered app
      try {
        // construct updated app
        var updatedApp = new SystemRemoteApps.SystemRemoteApp(
          name: app.Name ?? appName,
          fullName: app.FullName ?? registeredApp.FullName,
          path: app.Path ?? registeredApp.Path,
          vPath: app.VPath ?? registeredApp.VPath,
          iconPath: app.IconPath ?? registeredApp.IconPath,
          iconIndex: app.IconIndex ?? registeredApp.IconIndex,
          commandLine: app.CommandLine ?? registeredApp.CommandLine,
          commandLineOption: app.CommandLineOption ?? registeredApp.CommandLineOption,
          includeInWorkspace: app.IncludeInWorkspace ?? registeredApp.IncludeInWorkspace,
          fileTypeAssociations: app.FileTypeAssociations ?? registeredApp.FileTypeAssociations,
          securityDescriptor: app.securityDescriptor ?? registeredApp.securityDescriptor
        );
        updatedApp.WriteToRegistry();

        // if renaming, delete the old registry key
        if (isRenaming) {
          registeredApp.DeleteFromRegistry();
        }

        return Ok();
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
