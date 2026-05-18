using System.Runtime.InteropServices;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class CountImageIndicesEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/icon/indices", Handle);
  }

  /// <summary>
  /// Counts the number of images in the specified file. This is useful for
  /// files that can contain multiple icons, such as .exe and .dll files.
  /// Other supported image file types will always return 1.
  /// <br /><br />
  /// If the path is invalid, an error occurs, or the icon type is
  /// unsupported, a 404 or 500 status code is returned.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  private static IResult Handle(HttpContext ctx, string? path = null) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    // check if the path is a valid absolute path that exists
    if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || !File.Exists(path)) {
      return Results.NotFound();
    }

    var ext = Path.GetExtension(path);

    // count the number of icons in exe, dll, or ico file
    if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".ico", StringComparison.OrdinalIgnoreCase)) {
      try {
        // count the number of icons in the file
        var count = ExtractIconEx(path, -1, null, null, 0);
        return Results.Ok((int)count);
      }
      catch {
        return Results.Problem(statusCode: 500);
      }
    }

    // for other file types, it is probobly just a single image
    if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)) {
      return Results.Ok(1);
    }

    return Results.BadRequest("Unsupported image file type.");
  }

  [DllImport("shell32.dll", CharSet = CharSet.Auto)]
  private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[]? phiconLarge, [Out] IntPtr[]? phiconSmall, [In] uint nIcons);

}
