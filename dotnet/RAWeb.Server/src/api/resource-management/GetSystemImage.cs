using System.Runtime.InteropServices;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetSystemImageEndpoint {
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[]? phiconLarge, [Out] IntPtr[]? phiconSmall, [In] uint nIcons);

    [DllImport("user32.dll")]
    private static extern int DestroyIcon(IntPtr hIcon);

    internal static void Map(IEndpointRouteBuilder app) {
        app.MapGet("/api/management/resources/icon", Handle);
    }

    /// <summary>
    /// Serves an icon from specified rooted path. If an index is specified,
    /// serves the icon at that index or id within the file.
    /// <br /><br />
    /// If the path is invalid or an error occurs, the default icon is served
    /// instead. In these cases, although the icon is served, the HTTP status code
    /// will indicate an error (404 for invalid path and 500 for other errors).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private static IResult Handle(HttpContext ctx, string? path = null, string? index = null, string? fallback = null, string? theme = "light", string? frame = null) {
        var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
        if (userInfo is null || !userInfo.IsLocalAdministrator) {
            return Results.Forbid();
        }

        try {
            var rootedPath = path is not null && !Path.IsPathRooted(path)
                ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path))
                : path;
            var rootedFallback = !string.IsNullOrEmpty(fallback) && !Path.IsPathRooted(fallback)
                ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, fallback))
                : fallback;

            var imageTheme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
            var imageStream = ImageUtilities.ImagePathToStream(rootedPath, index, rootedFallback, imageTheme).ImageStream;
            if (imageStream is null) {
                return ServeDefaultIcon(500);
            }

            // insert the image into a PC monitor frame
            if (frame == "pc") {
                // compose the desktop icon with the wallpaper and overlay
                var composed = ImageUtilities.ComposeDesktopIcon(imageStream);
                imageStream.Dispose(); // we no longer need the original image stream
                imageStream = composed;
                if (imageStream is null) {
                    return Results.Problem("Error composing desktop icon.", statusCode: 500);
                }
            }

            var response = ImageUtilities.CreateResponse(imageStream);
            return ServeHttpResponseMessage(response);
        }
        catch (FileNotFoundException) {
            return ServeDefaultIcon(404);
        }
        catch (ImageUtilities.UnsupportedImageFormatException) {
            return ServeDefaultIcon(400);
        }
        catch {
            return ServeDefaultIcon(500);
        }
    }

    /// <summary>
    /// Serves the default icon.
    /// </summary>
    /// <returns></returns>
    private static BytesWithStatusCodeResult ServeDefaultIcon(int statusCode = 200) {
        using var stream = new FileStream(ImageUtilities.DefaultIconPath, FileMode.Open, FileAccess.Read);
        var response = ImageUtilities.CreateResponse(stream, (System.Net.HttpStatusCode)statusCode);
        return ServeHttpResponseMessage(response, statusCode);
    }

    /// <summary>
    /// Serves an HTTP response message containing image data and a status code. This is necessary
    /// to ensure that even when we serve a default icon due to an error, the
    /// HTTP status code will reflect the error (e.g. 404 or 500) instead of 200.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="statusCodeOverride"></param>
    /// <returns></returns>
    private static BytesWithStatusCodeResult ServeHttpResponseMessage(HttpResponseMessage response, int? statusCodeOverride = null) {
        // Results.Bytes does not accept a status code, so we write it via a stream result with explicit status
        var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
        var statusCode = statusCodeOverride ?? (int)response.StatusCode;
        return new BytesWithStatusCodeResult(bytes, contentType, statusCode);
    }

    private sealed class BytesWithStatusCodeResult(byte[] data, string contentType, int statusCode) : IResult {
        public Task ExecuteAsync(HttpContext httpContext) {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = contentType;
            httpContext.Response.ContentLength = data.Length;
            return httpContext.Response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
