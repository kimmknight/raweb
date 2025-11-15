using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

    /// <summary>
    /// Serves an icon from specified rooted path. If an index is specified,
    /// serves the icon at that index or id within the file.
    /// <br /><br />
    /// If the path is invalid or an error occurs, the default icon is served
    /// instead. In these cases, although the icon is served, the HTTP status code
    /// will indicate an error (404 for invalid path and 500 for other errors).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("icon")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetSystemIcon(string path, string index = null, string fallback = null, string theme = "light", string frame = null) {
      try {
        var rootedPath = !Path.IsPathRooted(path) && path != null ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path)) : path;
        var rootedFallbackPath = !string.IsNullOrEmpty(fallback) ? !Path.IsPathRooted(fallback) ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, fallback)) : null : null;
        var _theme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
        var imageStream = ImageUtilities.ImagePathToStream(rootedPath, index, rootedFallbackPath, _theme);

        // insert the image into a PC monitor frame
        if (frame == "pc") {
          // compose the desktop icon with the wallpaper and overlay
          var newImageStream = ImageUtilities.ComposeDesktopIcon(imageStream);
          imageStream.Dispose(); // we no longer need the original image stream
          imageStream = newImageStream;
          if (newImageStream == null) {
            return InternalServerError(new Exception("Error composing desktop icon."));
          }
          imageStream = newImageStream;
        }


        var response = ImageUtilities.CreateResponse(imageStream);
        return ResponseMessage(response);
      }
      catch (FileNotFoundException) {
        return ServeDefaultIcon(HttpStatusCode.NotFound);
      }
      catch (ImageUtilities.UnsupportedImageFormatException) {
        return ServeDefaultIcon(HttpStatusCode.BadRequest);
      }
      catch {
        return ServeDefaultIcon(HttpStatusCode.InternalServerError);
      }
    }

    /// <summary>
    /// Serves the default icon.
    /// </summary>
    /// <returns></returns>
    private IHttpActionResult ServeDefaultIcon(HttpStatusCode statusCode = HttpStatusCode.OK) {
      using (var defaultIconFileStream = new FileStream(ImageUtilities.DefaultIconPath, FileMode.Open, FileAccess.Read)) {
        var response = ImageUtilities.CreateResponse(defaultIconFileStream, statusCode);
        return ResponseMessage(response);
      }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, [In] uint nIcons);

    [DllImport("user32.dll")]
    public static extern int DestroyIcon(IntPtr hIcon);
  }
}
