using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class UnregisterAppEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapDelete("/api/management/resources/registered/{*identifier}", Handle);
  }

  /// <summary>
  /// Removes a registered RemoteApp application from the system registry.
  /// </summary>
  /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
  /// <returns></returns>
  private static IResult Handle(string identifier, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
    var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

    // find the resource
    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources();
    var resource = resources.TryGetByIdentifier(identifier);
    if (resource is null) {
      return Results.NotFound();
    }

    // remove the resource
    try {
      if (resource.Source == ManagedResourceSource.File) {
        // delete from managed resources folder
        var fsApp = (resource as ManagedFileResource)!;
        fsApp.Delete();
        return Results.Ok();
      }

      if (resource.Source == ManagedResourceSource.CentralPublishedResourcesDesktop) {
        return Results.Problem("The system desktop cannot be unregistered.", statusCode: 400);
      }

      try {
        var registryApp = (resource as SystemRemoteApps.SystemRemoteApp)!;
        registryApp.SetCollectionName(collectionName);
        ManagementServiceClient.Proxy.DeleteRemoteAppFromRegistry(registryApp);
        return Results.Ok();
      }
      catch (EndpointNotFoundException ex) {
        return Results.Problem(ex.Message, statusCode: 500);
      }
    }
    catch (Exception ex) {
      return Results.Problem(ex.Message, statusCode: 500);
    }
  }
}
