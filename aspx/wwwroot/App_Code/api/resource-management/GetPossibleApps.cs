using System.Security.Principal;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the list of installed applications on the system
    /// or for a specific user if a user SID is provided.
    /// <br /><br />
    /// This endpoint examines shortcuts in the Start Menu and
    /// packages installed via MSIX/APPX to determine the list of
    /// installed applications.
    /// <br /><br />
    /// Since installed packages are user-based, installed packages
    /// are not enumerated when no user SID is provided.
    /// </summary>
    /// <param name="userSid"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("available")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetPossibleApps(string userSid = null) {
      var remoteAppsUtil = new SystemRemoteApps();

      // get the installed apps for the entire system, or if a 
      // user SID is provided, for that specific user
      SystemRemoteApps.InstalledAppsCollection installedApps;
      if (string.IsNullOrEmpty(userSid)) {
        installedApps = remoteAppsUtil.GetInstalledApps();
      }
      else {
        var sid = new SecurityIdentifier(userSid);
        installedApps = remoteAppsUtil.GetInstalledAppsForUser(sid);
      }

      return Ok(installedApps);
    }
  }
}
