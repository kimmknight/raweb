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
      var remoteAppsUtil = new SystemRemoteApps();
      var apps = remoteAppsUtil.GetAllRegisteredApps();
      return Ok(apps);
    }
  }
}
