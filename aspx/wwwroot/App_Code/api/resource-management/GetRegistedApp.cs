using System;
using System.Configuration;
using System.ServiceModel;
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
      var supportsCentralizedPublishing = ConfigurationManager.AppSettings["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? Utilities.AppId.ToCollectionName() : null;
      var remoteAppsUtil = new SystemRemoteApps(collectionName);
      try {
        var app = remoteAppsUtil.GetRegistedApp(key);
        app.RdpFileString = Utilities.RegistryReader.ConstructRdpFileFromRegistry(key); // ensure the string is always populated
        return Ok(app);
      }

      // if we get an unauthorized access exception, try initializing
      // the registry paths via the management service before retrying
      catch (UnauthorizedAccessException) {
        try {
          SystemRemoteAppsClient.Proxy.InitializeRegistryPaths(collectionName);
          var app = remoteAppsUtil.GetRegistedApp(key);
          return Ok(app);
        }
        catch (EndpointNotFoundException) {
          return InternalServerError(new Exception("The RAWeb Management Service is not running."));
        }
      }
    }
  }
}
