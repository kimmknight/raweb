using System.Text.Json.Serialization;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetServerKeyboardLayoutEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/server-keyboard-layout", Handle);
  }

  /// <summary>
  /// Reports the keyboard layout that would be automatically detected and used by guacd
  /// for registry-based RemoteApps and the registry-based built-in desktop and also
  /// provides a list of all supported "server-layout" values that this version of the server supports.
  /// </summary>
  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    var (layout, _) = GuacdKeyboardLayouts.DetectServerLayout();
    return Results.Ok(new ServerKeyboardLayoutResponse(layout, GuacdKeyboardLayouts.All));
  }
}

public record ServerKeyboardLayoutResponse(
  [property: JsonPropertyName("layout")] string Layout,
  [property: JsonPropertyName("layouts")] IReadOnlyList<string> Layouts
);
