using System.Web.Http;
using RAWebServer.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the details of a registered RemoteApp application.
    /// </summary>
    /// <param name="appName"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("registered/{*appName}")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetRegistedApp(string appName) {
      var remoteAppsUtil = new SystemRemoteApps();
      var app = remoteAppsUtil.GetRegistedApp(appName);
      return Ok(app);
    }
  }
}
