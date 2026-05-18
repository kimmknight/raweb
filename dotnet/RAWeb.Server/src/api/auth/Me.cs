using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class MeEndpoint {
    internal static void Map(IEndpointRouteBuilder app) {
        app.MapGet("/api/auth/me", Handle);
    }

    private static IResult Handle(HttpContext ctx) {
        var ticket = AuthTicket.FromHttpRequestCookie(ctx.Request);
        if (ticket is null) {
            return Results.Unauthorized();
        }

        var userInfo = UserInformation.FromDownLevelLogonName(ticket.Name);
        if (userInfo is not null) {
            return Results.Ok(userInfo);
        }

        return Results.Ok(ticket.ToString());
    }
}
