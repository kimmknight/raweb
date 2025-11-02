using System;
using System.Configuration;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the details of all RemoteApps included in the registry.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("registered")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetRegistedApps() {
      var supportsCentralizedPublishing = ConfigurationManager.AppSettings["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? Utilities.AppId.ToCollectionName() : null;
      var remoteAppsUtil = new SystemRemoteApps(collectionName);
      try {
        var app = remoteAppsUtil.GetAllRegisteredApps();
        return Ok(app);
      }

      // if we get an unauthorized access exception, try initializing
      // the registry paths via the management service before retrying
      catch (UnauthorizedAccessException) {
        try {
          SystemRemoteAppsClient.Proxy.InitializeRegistryPaths(collectionName);
          var app = remoteAppsUtil.GetAllRegisteredApps();
          return Ok(app);
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
      }
    }
  }
}
