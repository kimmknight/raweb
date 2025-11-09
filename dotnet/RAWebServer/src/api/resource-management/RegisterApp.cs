using System;
using System.ServiceModel;
using System.Web.Http;
using Newtonsoft.Json;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Registers a new RemoteApp application in the registry or the managed resources folder.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("registered")]
    [RequireLocalAdministrator]
    public IHttpActionResult RegisterApp(System.Net.Http.HttpRequestMessage request) {
      // we need to manually deserialize because [FromBody] gets stuck in an infinite loop for ManagedResource for some reason
      var json = request.Content.ReadAsStringAsync().Result;
      var app = JsonConvert.DeserializeObject<ManagedResource>(json, new ManagedResourceDeserializer());
      if (app == null) {
        return BadRequest("Missing or invalid request body.");
      }

      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

      // load all registered apps to check for conflicts
      var resources = GetPopulatedManagedResources();

      // check if the app is already registered
      var alreadyExists = resources.TryGetByIdentifier(app.Identifier) != null;
      if (alreadyExists) {
        return Conflict();
      }

      // register the app
      try {
        if (app.Source == ManagedResourceSource.File) {
          var fsApp = app as ManagedFileResource;

          // save to managed resources folder
          var managedResourcesFolderPath = Constants.ManagedResourcesFolderPath;
          fsApp.WriteToFile();

          return Ok();
        }
        else {
          try {
            var registryApp = app as SystemRemoteApps.SystemRemoteApp;
            registryApp.SetCollectionName(collectionName);
            SystemRemoteAppsClient.Proxy.WriteRemoteAppToRegistry(registryApp);
            return Ok();
          }
          catch (EndpointNotFoundException) {
            return InternalServerError(new Exception("The RAWeb Management Service is not running."));
          }
        }
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
