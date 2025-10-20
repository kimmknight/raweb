using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Serves an icon from specified rooted path. If an index is specified,
    /// serves the icon at that index within the file.
    /// <br /><br />
    /// If the path is invalid or an error occurs, the default icon is served
    /// instead. In these cases, although the icon is served, the HTTP status code
    /// will indicate an error (404 for invalid path and 500 for other errors).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("icon")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetSystemIcon(string path, int index = 0) {
      // check if the path is a valid absolute path that exists
      var isValidPath = Path.IsPathRooted(path) && File.Exists(path);

      // if the path is invalid, return the default icon (but still use status 404)
      if (!isValidPath) {
        return ServeDefaultIcon(HttpStatusCode.NotFound);
      }

      // attempt to serve the icon from the specified path by extracting the embedded icon
      var isExeDllIco = Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
      if (isExeDllIco) {
        try {
          // extract the icon handle
          var phiconLarge = new IntPtr[1];
          ExtractIconEx(path, index, phiconLarge, null, 1);

          // convert the icon handle to an Icon object and save it to a MemoryStream
          var iconLarge = System.Drawing.Icon.FromHandle(phiconLarge[0]);
          var imageStream = new MemoryStream();
          iconLarge.ToBitmap().Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
          imageStream.Position = 0;

          // dispose the icon and handle
          DestroyIcon(phiconLarge[0]);
          iconLarge.Dispose();

          return ServeStream(imageStream);
        }
        // or serve the default icon on error
        catch {
          return ServeDefaultIcon(HttpStatusCode.InternalServerError);
        }
      }

      // for other file types, attempt to serve the image file directly
      var isSupportedFileType = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".bmp", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase);
      if (isSupportedFileType) {
        try {
          var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
          return ServeStream(fileStream);
        }
        // or serve the default icon on error
        catch {
          return ServeDefaultIcon(HttpStatusCode.InternalServerError);
        }
      }

      // if the file type is unsupported, serve the default icon
      return ServeDefaultIcon(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Serves the default icon.
    /// </summary>
    /// <returns></returns>
    private IHttpActionResult ServeDefaultIcon(HttpStatusCode statusCode = HttpStatusCode.OK) {
      var defaultIconPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "lib",
        "assets",
        "default.ico"
      );

      using (var defaultIconFileStream = new FileStream(defaultIconPath, FileMode.Open, FileAccess.Read)) {
        return ServeStream(defaultIconFileStream, statusCode);
      }
    }

    /// <summary>
    /// Serves a PNG image stream. If the icon is larger than 256x256, it is resized.
    /// </summary>
    /// <param name="icon"></param>
    /// <returns></returns>
    private IHttpActionResult ServeStream(Stream icon, HttpStatusCode statusCode = HttpStatusCode.OK) {
      var resourceController = new ResourceController();
      var outputDimensions = resourceController.GetResizedDimensionsFromMaxSize(icon, 256);
      var stream = resourceController.ResizeImage(icon, outputDimensions.Width, outputDimensions.Height);

      // build HTTP response
      var response = new HttpResponseMessage(statusCode) {
        Content = new StreamContent(stream)
      };

      response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

      // if length is known, set Content-Length
      if (stream.CanSeek) {
        response.Content.Headers.ContentLength = stream.Length;
      }

      return ResponseMessage(response);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, [In] uint nIcons);

    [DllImport("user32.dll")]
    public static extern int DestroyIcon(IntPtr hIcon);
  }
}
