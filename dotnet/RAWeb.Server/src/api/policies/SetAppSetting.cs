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

    // read the value as plain text from the request body
    using var reader = new StreamReader(ctx.Request.Body);
    var value = await reader.ReadToEndAsync();

    PoliciesManager.Set(key, value);
    return Results.Ok();
  }
}
