using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class MeEndpoint {
    internal static void Map(IEndpointRouteBuilder app) {
        app.MapGet("/api/auth/me", Handle);
    }

    private static IResult Handle(HttpContext ctx) {
        var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request, writeAccess: true);
        if (userInfo is null) {
            return Results.Unauthorized();
        }

        return Results.Ok(userInfo);
    }
}
