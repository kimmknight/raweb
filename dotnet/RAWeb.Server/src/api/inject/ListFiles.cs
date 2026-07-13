using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Win32;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ListInjectFilesEndpoint {
  private static readonly FileExtensionContentTypeProvider s_contentTypeProvider = new();

  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/inject/list-files", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    var filestorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", "filestore");
    if (!Directory.Exists(filestorePath)) {
      return Results.Ok(Array.Empty<InjectFileItem>());
    }

    // use the PathBase so the URL is correct when RAWeb is hosted under a sub-path in IIS
    var controllerPathname = (ctx.Request.PathBase.HasValue ? ctx.Request.PathBase.Value : "") + "/api/inject/file/";

    var files = Directory
        .GetFiles(filestorePath, "*", SearchOption.AllDirectories)
        .Where(filePath => FileAccessInfo.CanAccessPath(filePath, userInfo!))
        .Select(filePath => {
          var relativePath = filePath
                  .Substring(filestorePath.Length)
                  .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                  .Replace('\\', '/');
          var url = controllerPathname + relativePath.Replace('\\', '/');

          if (relativePath.StartsWith("public/", StringComparison.OrdinalIgnoreCase)) {
            return null; // skip public folder files
          }

          if (!s_contentTypeProvider.TryGetContentType(filePath, out var mimeType)) {
            mimeType = "application/octet-stream";
          }
          var mimeTypeLabel = GetFileTypeLabel(filePath);

          var dateModified = File.GetLastWriteTimeUtc(filePath);

          // Use the URL from .url files instead of the URL to the file path
          if (mimeType == "application/octet-stream" && Path.GetExtension(filePath).Equals(".url", StringComparison.OrdinalIgnoreCase)) {
            try {
              var lines = File.ReadAllLines(filePath);
              var urlLine = lines.FirstOrDefault(line => line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));
              if (urlLine is not null) {
                url = urlLine.Substring(4).Trim();
                mimeType = "text/url";
              }
            }
            catch { }
          }

          return new InjectFileItem(
            Name: Path.GetFileNameWithoutExtension(filePath),
            Url: url,
            MimeType: mimeType,
            FileType: mimeTypeLabel,
            DateModified: dateModified,
            Icon: GetShellIconDataUrl(filePath)
          );
        })
        .OfType<InjectFileItem>()
        .ToArray();

    return Results.Ok(files);
  }

  private static string? GetShellIconDataUrl(string filePath) {
    try {
      using var icon = Icon.ExtractAssociatedIcon(filePath);
      using var bitmap = icon!.ToBitmap();
      using var stream = new MemoryStream();
      bitmap.Save(stream, ImageFormat.Png);
      return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
    }
    catch {
      return null;
    }
  }

  private static string GetFileTypeLabel(string filePath) {
    var ext = Path.GetExtension(filePath).ToLower();

    using var key = Registry.ClassesRoot.OpenSubKey(ext);
    if (key?.GetValue(null) is not string progId) {
      return ext;
    }

    using var progKey = Registry.ClassesRoot.OpenSubKey(progId);
    return progKey?.GetValue(null) as string ?? progId;
  }
}

public record InjectFileItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("mimeType")] string? MimeType,
    [property: JsonPropertyName("fileType")] string? FileType,
    [property: JsonPropertyName("dateModified")] DateTime DateModified,
    [property: JsonPropertyName("icon")] string? Icon
);
