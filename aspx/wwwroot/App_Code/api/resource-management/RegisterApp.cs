using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Registers a new RemoteApp application in the registry.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("registered")]
    [RequireLocalAdministrator]
    public IHttpActionResult RegisterApp([FromBody] SystemRemoteApps.SystemRemoteApp app) {
      var remoteAppsUtil = new SystemRemoteApps();

      if (app == null) {
        return BadRequest("Missing or invalid request body.");
      }

      // check if the app is already registered
      var alreadyExists = remoteAppsUtil.GetRegistedApp(app.Key) != null;
      if (alreadyExists) {
        return Conflict();
      }

      // register the app
      try {
        try {
          SystemRemoteAppsClient.Proxy.WriteRemoteAppToRegistry(app);
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
