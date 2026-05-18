using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class AuthenticateWorkspaceEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/auth/authenticate-workspace", Handle);
    app.MapGet("/auth/loginfeed.aspx", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    // check if workspace authentication is blocked via policy
    var blockWorkspaceAuth = PoliciesManager.RawPolicies["WorkspaceAuth.Block"] == "true";
    if (blockWorkspaceAuth) {
      return Results.Ok(
        new WorkspaceBlockedResponse(
          success: false,
          error: "Workspace client authentication is blocked by policy."
        )
      );
    }

    if (ShouldAuthenticateAnonymously()) {
      var anonTicket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser);
      return CreateWorkspaceAuthResponse(anonTicket);
    }

    var ticket = AuthTicket.FromHttpRequestIdentity(ctx.Request);
    return CreateWorkspaceAuthResponse(ticket);
  }

  private static IResult CreateWorkspaceAuthResponse(AuthTicket ticket) {
    return Results.Content(ticket.ToEncryptedToken(), "application/x-msts-webfeed-login; charset=utf-8");
  }

  private static bool ShouldAuthenticateAnonymously() {
    var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
    return anonSetting == "always";
  }
}

// camelCase property names to match the original RAWebServer JSON output
public class WorkspaceBlockedResponse(bool success, string error) {
  [JsonPropertyName("success")] public bool Success { get; } = success;
  [JsonPropertyName("error")] public string Error { get; } = error;
}
