using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class SetAppSettingEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/policies/{key}", Handle);
  }

  private static async Task<IResult> Handle(string key, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request, writeAccess: true);
    if (userInfo is null || userInfo.AuthTicketLevel != AuthTicketLevel.ReadAndWriteAdmin) {
      return Results.Forbid();
    }

    if (PoliciesManager.IsFileOnlyPolicy(key)) {
      return Results.BadRequest("This policy can only be changed by editing appSettings.config on the server.");
    }

    // require the value to be a JSON string so that browsers send a CORS preflight request
    // before cross-origin requests, which prevents forms on other sites from changing policies
    if (!ctx.Request.HasJsonContentType()) {
      return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }

    string? value;
    try {
      value = await ctx.Request.ReadFromJsonAsync(WebApiJsonSerializerContext.Default.String);
    }
    catch (JsonException) {
      return Results.BadRequest("The request body must be a JSON string.");
    }

    // secrets are redacted when policies are read, so keep the stored
    // secret for any connection that still has the redacted secret
    try {
      value = PoliciesManager.RestoreRedactedSecrets(key, value ?? "");
    }
    catch (ArgumentException ex) {
      return Results.BadRequest(ex.Message);
    }

    PoliciesManager.Set(key, value);
    return Results.Ok();
  }
}
