using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetAppSettingsEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/policies", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    return Results.Ok(PoliciesManager.RawPolicies.Value);
  }
}
