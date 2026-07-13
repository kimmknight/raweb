using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ClearEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/auth/clear", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    // expire auth cookie on the client
    if (ctx.Request.Cookies.ContainsKey(Constants.DefaultAuthCookieName)) {
      ctx.Response.Cookies.Append(
          Constants.DefaultAuthCookieName,
          "",
          new CookieOptions {
            Path = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase + "/" : "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
          }
      );
    }

    return Results.Ok(new ClearResponse(true));
  }
}

public record ClearResponse([property: JsonPropertyName("cleared")] bool Cleared);
