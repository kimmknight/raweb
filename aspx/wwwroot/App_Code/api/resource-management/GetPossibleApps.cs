using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
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
    /// To include applications from a specific user's Start Menu,
    /// provide the user's SID in the `userSid` parameter.
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
      IEnumerable<InstalledApp> installedApps;
      if (string.IsNullOrEmpty(userSid)) {
        try {
          installedApps = SystemRemoteAppsClient.Proxy.ListInstalledApps();
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
        installedApps = ExcludeDuplicatesDocumentationAndUninstallers(installedApps).ToList();
      }
      else {
        try {
          installedApps = SystemRemoteAppsClient.Proxy.ListInstalledApps(userSid);
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
        installedApps = ExcludeDuplicatesDocumentationAndUninstallers(installedApps).ToList();
      }

      return Ok(installedApps);
    }

    private IEnumerable<InstalledApp> ExcludeDuplicatesDocumentationAndUninstallers(IEnumerable<InstalledApp> apps) {
      var seen = new HashSet<string>();
      foreach (var app in apps) {
        var isDocumentation = app.DisplayName.ToLower().Contains("documentation") ||
                              app.DisplayName.ToLower().Contains("docs") ||
                              app.DisplayName.ToLower().Contains("readme") ||
                              app.DisplayName.ToLower().Contains("about") ||
                              app.DisplayName.ToLower().Contains("release notes") ||
                              app.DisplayName.ToLower().Contains("help");

        var isUninstaller = app.DisplayName.ToLower().Contains("uninstall") ||
                            app.DisplayName.ToLower().Contains("remove");

        // skip documentation and uninstaller apps
        if (isDocumentation || isUninstaller) {
          continue;
        }

        // skip duplicate apps based on their display name
        if (seen.Contains(app.DisplayName)) {
          continue;
        }

        seen.Add(app.DisplayName);
        yield return app;
      }
    }
  }
}
