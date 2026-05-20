using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetPossibleAppsEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/available", Handle);
  }

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
  private static IResult Handle(HttpContext ctx, string? userSid = null) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    try {
      var installedApps = string.IsNullOrEmpty(userSid)
          ? ManagementServiceClient.Proxy.ListInstalledApps()
          : ManagementServiceClient.Proxy.ListInstalledApps(userSid);

      return Results.Ok(installedApps);
    }
    catch (EndpointNotFoundException ex) {
      return Results.Problem(ex.Message, statusCode: 500);
    }
  }
}
