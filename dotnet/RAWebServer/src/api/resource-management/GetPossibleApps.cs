using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

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
    /// To include applications from a specific user's Start Menu,
    /// provide the user's SID in the `userSid` parameter.
    /// </summary>
    /// <param name="userSid"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("available")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetPossibleApps(string userSid = null) {
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;
      var remoteAppsUtil = new SystemRemoteApps(collectionName);

      // get the installed apps for the entire system, or if a 
      // user SID is provided, for that specific user
      IEnumerable<InstalledApp> installedApps;
      if (string.IsNullOrEmpty(userSid)) {
        try {
          installedApps = SystemRemoteAppsClient.Proxy.ListInstalledApps();
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
      }
      else {
        try {
          installedApps = SystemRemoteAppsClient.Proxy.ListInstalledApps(userSid);
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
      }

      return Ok(installedApps);
    }
  }
}
