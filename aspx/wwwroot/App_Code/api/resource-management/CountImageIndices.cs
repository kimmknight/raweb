using System;
using System.IO;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class ResourceManagementController : ApiController {

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
    [HttpGet]
    [Route("icon/indices")]
    [RequireLocalAdministrator]
    public IHttpActionResult CountImageIndices(string path) {
      // check if the path is a valid absolute path that exists
      var isValidPath = Path.IsPathRooted(path) && File.Exists(path);
      if (!isValidPath) {
        return NotFound();
      }

      // count the number of icons in exe, dll, or ico file
      var isExeDllIco = Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
      if (isExeDllIco) {
        try {
          // count the number of icons in the file
          var iconsCount = ExtractIconEx(path, -1, null, null, 0);
          return Ok(iconsCount);
        }
        catch {
          return InternalServerError();
        }
      }

      // for other file types, it is probobly just a single image
      var isSupportedFileType = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".bmp", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase);
      if (isSupportedFileType) {
        return Ok(1);
      }

      return BadRequest("Unsupported image file type.");
    }
  }
}
