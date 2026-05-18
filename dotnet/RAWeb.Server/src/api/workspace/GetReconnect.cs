using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetReconnectEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/workspace/reconnect", Handle);
    app.MapPost("/api/workspace/reconnect", Handle);
    app.MapGet("/RDWebService.asmx", Handle);
    app.MapPost("/RDWebService.asmx", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Redirect("/api/auth/authenticate-workspace");
    }

    return Results.Content(WorkspaceReconnectStubXml, "text/xml");
  }

  /// <summary>
  /// A stub response for MS-RDWR. RAWeb cannot track which RDP files are in use
  /// across all terminal servers, so this just provides a valid empty response.
  ///
  /// NOTE: there MUST NOT be any whitespace before the XML or MS-RDWR will reject it.
  /// </summary>
  private const string WorkspaceReconnectStubXml =
      "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
      "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
        "<soap:Body>" +
          "<GetRDPFilesResponse xmlns=\"http://schemas.microsoft.com/ts/2010/09/rdweb\">" +
            "<GetRDPFilesResult>" +
              "<version>8.0</version>" +
              "<wkspRC></wkspRC>" +
            "</GetRDPFilesResult>" +
          "</GetRDPFilesResponse>" +
        "</soap:Body>" +
      "</soap:Envelope>";
}
