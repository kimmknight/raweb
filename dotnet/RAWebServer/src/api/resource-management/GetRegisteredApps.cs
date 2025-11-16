using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the details of all RemoteApps included in the registry and all
    /// RemoteApps and desktops included in App_Data/managed-resources.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("registered")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetRegistedApps() {
      var resources = GetPopulatedManagedResources();
      return Ok(resources);
    }

    /// <summary>
    /// Gets a populated collection of all registered RemoteApps and managed resources.
    /// <br /><br />
    /// This method is used internally to avoid code duplication in multiple endpoints.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private ManagedResources GetPopulatedManagedResources() {
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

      var resources = new ManagedResources();

      try {
        resources.Populate(collectionName, Constants.ManagedResourcesFolderPath, restorePackagedAppIconPaths: true);
      }
      catch (UnauthorizedAccessException) {
        try {
          SystemRemoteAppsClient.Proxy.InitializeRegistryPaths(collectionName);
          SystemRemoteAppsClient.Proxy.InitializeDesktopRegistryPaths(collectionName);
          SystemRemoteAppsClient.Proxy.RestorePackagedAppIconPaths(collectionName);
          resources.Populate(collectionName, Constants.ManagedResourcesFolderPath);
        }
        catch (EndpointNotFoundException) {
          throw new Exception("The RAWeb Management Service is not running.");
        }
      }

      return resources;
    }
  }
}
