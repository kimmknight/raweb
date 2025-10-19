using System;
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
      var remoteAppsUtil = new SystemRemoteApps();
      var app = remoteAppsUtil.GetRegistedApp(key);
      try {
        app.DeleteFromRegistry();
        return Ok();
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
