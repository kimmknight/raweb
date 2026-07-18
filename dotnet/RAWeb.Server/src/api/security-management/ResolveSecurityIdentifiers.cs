using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ResolveSecurityIdentifiersEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/management/security/resolve-sids", Handle);
  }

  /// <summary>
  /// Resolves a list of security identifiers (SIDs) to their corresponding
  /// account domains, usernames, and display names.
  /// <br /><br />
  /// See <see cref="ResolvedSecurityIdentifiers.FromSidStrings(string[], out List{string})"/> for details.
  /// </summary>
  /// <param name="body"></param>
  /// <returns></returns>
  private static IResult Handle(string[] body, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.AuthTicketLevel.IsAdmin) {
      return Results.Forbid();
    }

    if (body is null || body.Length == 0) {
      return Results.BadRequest("No SIDs provided.");
    }

    var resolvedSids = ResolvedSecurityIdentifiers.FromSidStrings(body, out var invalidOrUnfoundSids);

    return Results.Ok(new ResolveSidsResponse(resolvedSids, invalidOrUnfoundSids));
  }
}

public record ResolveSidsResponse(IList<ResolvedSecurityIdentifier> ResolvedSids, List<string> InvalidOrUnfoundSids);
