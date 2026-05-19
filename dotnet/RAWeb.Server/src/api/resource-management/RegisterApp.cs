using System.Text.Json;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class RegisterAppEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/management/resources/registered", (Delegate)Handle);
  }

  /// <summary>
  /// Registers a new RemoteApp application in the registry or the managed resources folder.
  /// </summary>
  /// <returns></returns>
  private static async Task<IResult> Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    // read raw body and deserialize it
    var json = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    ManagedResourceJsonConverter.RootedManagedResourcesPath = Constants.ManagedResourcesFolderPath;
    var resource = JsonSerializer.Deserialize(json, WebApiJsonSerializerContext.Default.ManagedResource);
    if (resource is null) {
      return Results.BadRequest("Missing or invalid request body.");
    }

    var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
    var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

    // load all registered apps to check for conflicts
    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources();

    // check if the app is already registered
    if (resources.TryGetByIdentifier(resource.Identifier) is not null) {
      return Results.Conflict();
    }

    // register the app
    try {
      if (resource.Source == ManagedResourceSource.File) {
        var fsApp = (resource as ManagedFileResource)!;

        // save to managed resources folder
        var managedResourcesFolderPath = Constants.ManagedResourcesFolderPath;
        fsApp.WriteToFile();

        return Results.Ok();
      }
      else {
        try {
          var registryApp = (resource as SystemRemoteApps.SystemRemoteApp)!;
          registryApp.SetCollectionName(collectionName);
          ManagementServiceClient.Proxy.WriteRemoteAppToRegistry(registryApp);
          return Results.Ok();
        }
        catch (EndpointNotFoundException) {
          return Results.Problem("The RAWeb Management Service is not running.", statusCode: 500);
        }
      }
    }
    catch (Exception ex) {
      return Results.Problem(ex.Message, statusCode: 500);
    }
  }
}
