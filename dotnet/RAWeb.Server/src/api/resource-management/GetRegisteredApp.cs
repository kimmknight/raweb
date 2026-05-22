using System.Text.Json;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetRegisteredAppEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/registered/{*identifier}", Handle);
  }

  /// <summary>
  /// Gets the details of a registered RemoteApp or desktop.
  /// </summary>
  /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
  /// <returns></returns>
  private static IResult Handle(string identifier, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources();
    var app = resources.GetByIdentifier(identifier);

    // ensure the rdp file string is always populated
    if (
      app.Source == ManagedResourceSource.CentralPublishedResourcesApp ||
      app.Source == ManagedResourceSource.TSAppAllowList
    ) {
      app.RdpFileString = RegistryReader.ConstructRdpFileFromRegistry(identifier, httpContext: ctx);
    }
    else if (
      app.Source == ManagedResourceSource.CentralPublishedResourcesDesktop
    ) {
      app.RdpFileString = RegistryReader.ConstructRdpFileFromRegistry(identifier, isDesktop: true, httpContext: ctx);
    }
    else {
      app.RdpFileString = app.ToRdpFileStringBuilder(null).ToString();
    }

    return Results.Content(JsonSerializer.Serialize(app, WebApiJsonSerializerContext.Default.ManagedResource), "application/json");
  }
}
