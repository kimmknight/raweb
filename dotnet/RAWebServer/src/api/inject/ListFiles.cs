using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using Microsoft.Win32;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class InjectFolderFilesController : ApiController {
    [HttpGet]
    [Route("list-files")]
    [RequireAuthentication]
    public IHttpActionResult ListFiles() {
      var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);

      var filestoreFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", "filestore");
      if (!Directory.Exists(filestoreFolderPath)) {
        return Ok(new object[0]);
      }

      var controllerPathname = new Uri(Url.Content("~/api/inject/file/"), UriKind.Absolute).AbsolutePath;

      var files = Directory.GetFiles(filestoreFolderPath, "*", SearchOption.AllDirectories)
        .Where(filePath => FileAccessInfo.CanAccessPath(filePath, userInfo))
        .Select(filePath => {
          var relativePath = filePath
            .Substring(filestoreFolderPath.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');
          var url = controllerPathname + relativePath.Replace('\\', '/');

          var mimeType = MimeMapping.GetMimeMapping(filePath);
          var mimeTypeLabel = GetFileTypeLabel(filePath);

          var dateModified = File.GetLastWriteTimeUtc(filePath);

          // Use the URL from .url files instead of the URL to the file path
          if (mimeType == "application/octet-stream" && Path.GetExtension(filePath).Equals(".url", StringComparison.OrdinalIgnoreCase)) {
            try {
              var lines = File.ReadAllLines(filePath);
              var urlLine = lines.FirstOrDefault(line => line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));
              if (urlLine != null) {
                url = urlLine.Substring(4).Trim();
                mimeType = "text/url";
              }
            }
            catch {
            }
          }

          return new {
            name = Path.GetFileNameWithoutExtension(filePath),
            url,
            mimeType,
            fileType = mimeTypeLabel,
            dateModified,
            icon = GetShellIconDataUrl(filePath)
          };
        })
        .ToArray();

      return Ok(files);
    }

    private static string GetShellIconDataUrl(string filePath) {
      try {
        using (var icon = Icon.ExtractAssociatedIcon(filePath))
        using (var bitmap = icon.ToBitmap())
        using (var stream = new MemoryStream()) {
          bitmap.Save(stream, ImageFormat.Png);
          return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }
      }
      catch {
        return null;
      }
    }

    private string GetFileTypeLabel(string filePath) {
      var ext = Path.GetExtension(filePath).ToLower();

      using var key = Registry.ClassesRoot.OpenSubKey(ext);
      if (key?.GetValue(null) is not string progId) {
        return ext;
      }

      using var progKey = Registry.ClassesRoot.OpenSubKey(progId);
      return progKey?.GetValue(null) as string ?? progId;
    }
  }
}
