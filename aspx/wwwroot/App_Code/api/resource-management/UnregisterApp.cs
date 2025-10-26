using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Removes a registered RemoteApp application from the system registry.
    /// </summary>
    /// <param name="key">The key for the RemoteApp in the registry</param>
    /// <returns></returns>
    [HttpDelete]
    [Route("registered/{*key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult UnregisterApp(string key) {
      var collectionName = Utilities.AppId.ToCollectionName();
      var remoteAppsUtil = new SystemRemoteApps(collectionName);
      var app = remoteAppsUtil.GetRegistedApp(key);
      try {
        try {
          SystemRemoteAppsClient.Proxy.DeleteRemoteAppFromRegistry(app);
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
