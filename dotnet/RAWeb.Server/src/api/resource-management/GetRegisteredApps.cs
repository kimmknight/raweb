using System.Text.Json;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetRegisteredAppsEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/registered", Handle);
  }

  /// <summary>
  /// Gets the details of all RemoteApps included in the registry and all
  /// RemoteApps and desktops included in App_Data/managed-resources.
  /// </summary>
  /// <returns></returns>
  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var resources = GetPopulatedManagedResources();
    return Results.Content(JsonSerializer.Serialize(resources, WebApiJsonSerializerContext.Default.ManagedResources), "application/json");
  }

  /// <summary>
  /// Gets a populated collection of all registered RemoteApps and managed resources.
  /// <br /><br />
  /// This method is used internally to avoid code duplication in multiple endpoints.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  internal static ManagedResources GetPopulatedManagedResources() {
    var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
    var collectionName = supportsCentralizedPublishing ? AppId.ToCollectionName() : null;

    var resources = new ManagedResources();

    try {
      resources.Populate(collectionName, Constants.ManagedResourcesFolderPath, restorePackagedAppIconPaths: true);
    }
    catch (UnauthorizedAccessException) {
      try {
        ManagementServiceClient.Proxy.InitializeRegistryPaths(collectionName);
        ManagementServiceClient.Proxy.InitializeDesktopRegistryPaths(collectionName!);
        ManagementServiceClient.Proxy.RestorePackagedAppIconPaths(collectionName);
        resources.Populate(collectionName, Constants.ManagedResourcesFolderPath);
      }
      catch (EndpointNotFoundException) {
        throw new Exception("The RAWeb Management Service is not running.");
      }
    }

    return resources;
  }
}
