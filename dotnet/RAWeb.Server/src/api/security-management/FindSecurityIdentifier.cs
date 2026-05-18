using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class FindSecurityIdentifierEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/security/find-sid", Handle);
    app.MapPost("/api/management/security/find-sid", Handle);
  }

  /// <summary>
  /// Accepts a lookup string and attempts to find the corresponding
  /// resolved security identifier (SID) for a user or group.
  /// <br /><br />
  /// See <see cref="ResolvedSecurityIdentifier.FromLookupString(string, string?)"/> for details.
  /// </summary>
  /// <param name="lookup"></param>
  /// <param name="domain"></param>
  /// <returns></returns>
  private static IResult Handle(HttpContext ctx, string? lookup = null, string? domain = null) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(domain)) {
      domain = Environment.MachineName;
    }

    if (string.IsNullOrWhiteSpace(lookup)) {
      return Results.BadRequest("No username, group name, user principal name, or SID provided.");
    }

    try {
      var resolvedSid = ResolvedSecurityIdentifier.FromLookupString(lookup, domain);
      if (resolvedSid is null) {
        return Results.NotFound();
      }
      return Results.Ok(resolvedSid);
    }
    catch (Exception ex) {
      return Results.Problem(ex.Message);
    }
  }
}
