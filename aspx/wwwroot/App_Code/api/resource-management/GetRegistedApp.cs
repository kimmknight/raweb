using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the details of a registered RemoteApp application.
    /// </summary>
    /// <param name="key">The key for the RemoteApp in the registry</param>
    /// <returns></returns>
    [HttpGet]
    [Route("registered/{*key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetRegistedApp(string key) {
      var remoteAppsUtil = new SystemRemoteApps();
      var app = remoteAppsUtil.GetRegistedApp(key);
      return Ok(app);
    }
  }
}
