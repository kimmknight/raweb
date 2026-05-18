using System.Text;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetWorkspaceEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/workspace", Handle);
    app.MapGet("/webfeed.aspx", Handle);
  }

  private static IResult Handle(HttpContext ctx, string? terminalServer = "", string? mergeTerminalServers = "0") {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Redirect("/api/auth/authenticate-workspace");
    }

    var schemaVersion = WorkspaceBuilder.SchemaVersion.v1;
    var acceptHeader = ctx.Request.Headers.Accept.ToString().ToLowerInvariant();
    if (acceptHeader.Contains("radc_schema_version=2.0")) {
      schemaVersion = WorkspaceBuilder.SchemaVersion.v2;
    }
    else if (acceptHeader.Contains("radc_schema_version=2.1")) {
      schemaVersion = WorkspaceBuilder.SchemaVersion.v2_1;
    }

    try {
      var iisBase = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase + "/" : "/";
      var workspaceXml = new WorkspaceBuilder(
        schemaVersion,
        userInfo,
        ctx.Request.Host.Host,
        mergeTerminalServers == "1",
        terminalServer ?? "",
        iisBase,
        ManagementServiceClient.Proxy
      ).GetWorkspaceXmlString("resources", "multiuser-resources");

      var contentType = schemaVersion >= WorkspaceBuilder.SchemaVersion.v2
        ? "application/x-msts-radc+xml"
        : "text/xml";

      return Results.Content(workspaceXml, contentType, Encoding.UTF8);
    }
    catch (EndpointNotFoundException) {
      throw new Exception("The RAWeb Management Service is not running.");
    }
  }
}
