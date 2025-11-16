using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Removes a registered RemoteApp application from the system registry.
    /// </summary>
    /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
    /// <returns></returns>
    [HttpDelete]
    [Route("registered/{*identifier}")]
    [RequireLocalAdministrator]
    public IHttpActionResult UnregisterApp(string identifier) {
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

      // find the resource
      var resources = GetPopulatedManagedResources();
      var app = resources.GetByIdentifier(identifier);
      if (app == null) {
        return NotFound();
      }

      // remove the resource
      try {
        if (app.Source == ManagedResourceSource.File) {
          // delete from managed resources folder
          var fsApp = app as ManagedFileResource;
          fsApp.Delete();
          return Ok();
        }

        if (app.Source == ManagedResourceSource.CentralPublishedResourcesDesktop) {
          throw new Exception("The system desktop cannot be unregistered.");
        }

        try {
          var registryApp = app as SystemRemoteApps.SystemRemoteApp;
          registryApp.SetCollectionName(collectionName);
          SystemRemoteAppsClient.Proxy.DeleteRemoteAppFromRegistry(registryApp);
          return Ok();
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }

      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
