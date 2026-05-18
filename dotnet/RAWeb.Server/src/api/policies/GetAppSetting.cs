using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetAppSettingEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/policies/{key}", Handle);
  }

  private static IResult Handle(string key, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var value = PoliciesManager.RawPolicies[key];
    return Results.Ok(new AppSettingResponse(key, value));
  }
}

public record AppSettingResponse(string Key, string? Value);
