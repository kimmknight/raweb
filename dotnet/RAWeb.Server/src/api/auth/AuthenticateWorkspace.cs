using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class AuthenticateWorkspaceEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/auth/authenticate-workspace", Handle).RequireAuthorization("WindowsAuth");
    app.MapGet("/auth/loginfeed.aspx", Handle).RequireAuthorization("WindowsAuth");
  }

  private static IResult Handle(HttpContext ctx) {
    // check if workspace authentication is blocked via policy
    var blockWorkspaceAuth = PoliciesManager.RawPolicies["WorkspaceAuth.Block"] == "true";
    if (blockWorkspaceAuth) {
      return Results.Json(
        new WorkspaceBlockedResponse(
          success: false,
          error: "Workspace client authentication is blocked by policy."
        ),
        WebApiJsonSerializerContext.Default.WorkspaceBlockedResponse,
        statusCode: StatusCodes.Status403Forbidden
      );
    }

    if (ShouldAuthenticateAnonymously()) {
      var anonTicket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyWorkspaceUser);
      return CreateWorkspaceAuthResponse(anonTicket);
    }

    // ReadOnlyWorkspaceUser only allow access to endpoints required for functional workspace subscription
    var ticket = AuthTicket.FromHttpRequestIdentity(ctx.Request, maxLevel: AuthTicketLevel.ReadOnlyWorkspaceUser);
    return CreateWorkspaceAuthResponse(ticket);
  }

  private static IResult CreateWorkspaceAuthResponse(AuthTicket ticket) {
    // the ticket is only accepted while its session exists
    new AuthSessionStore().Add(ticket);
    return Results.Content(ticket.ToEncryptedToken(), "application/x-msts-webfeed-login; charset=utf-8");
  }

  private static bool ShouldAuthenticateAnonymously() {
    var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
    return anonSetting == "always";
  }
}

public class WorkspaceBlockedResponse(bool success, string error) {
  [JsonPropertyName("success")] public bool Success { get; } = success;
  [JsonPropertyName("error")] public string Error { get; } = error;
}
