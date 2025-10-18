using System;
using System.Web.Http;
using RAWebServer.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Registers a new RemoteApp application in the registry.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("registered")]
    [RequireLocalAdministrator]
    public IHttpActionResult RegisterApp([FromBody] SystemRemoteApps.SystemRemoteApp app) {
      var remoteAppsUtil = new SystemRemoteApps();

      // check if the app is already registered
      var alreadyExists = remoteAppsUtil.GetRegistedApp(app.Name) != null;
      if (alreadyExists) {
        return Conflict();
      }

      // register the app
      try {
        app.WriteToRegistry();
        return Ok();
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
