using System;
using System.Web.Http;
using RAWebServer.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Removes a registered RemoteApp application from the system registry.
    /// </summary>
    /// <param name="appName"></param>
    /// <returns></returns>
    [HttpDelete]
    [Route("registered/{*appName}")]
    [RequireLocalAdministrator]
    public IHttpActionResult UnregisterApp(string appName) {
      var remoteAppsUtil = new SystemRemoteApps();
      var app = remoteAppsUtil.GetRegistedApp(appName);
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
