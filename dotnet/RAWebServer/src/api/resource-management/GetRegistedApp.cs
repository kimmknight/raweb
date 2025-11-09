using System;
using System.ServiceModel;
using System.Web.Http;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Gets the details of a registered RemoteApp or desktop.
    /// </summary>
    /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
    /// <returns></returns>
    [HttpGet]
    [Route("registered/{*identifier}")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetRegistedApp(string identifier) {
      var resources = GetPopulatedManagedResources();
      var app = resources.GetByIdentifier(identifier);

      // ensure the rdp file string is always populated
      if (
        app.Source == ManagedResourceSource.CentralPublishedResourcesApp ||
        app.Source == ManagedResourceSource.TSAppAllowList
      ) {
        app.RdpFileString = RegistryReader.ConstructRdpFileFromRegistry(identifier);
      }

      return Ok(app);
    }
  }
}
