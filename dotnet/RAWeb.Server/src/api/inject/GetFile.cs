using Microsoft.AspNetCore.StaticFiles;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetInjectFileEndpoint {

  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/inject/file/{*relativeFilePath}", Handle);
  }

  private static readonly FileExtensionContentTypeProvider s_contentTypeProvider = new();

  private static IResult Handle(string relativeFilePath, HttpContext ctx) {
    if (string.IsNullOrEmpty(relativeFilePath)) {
      return Results.BadRequest("Missing relative file path parameter.");
    }

    var injectRoot = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, "inject"));
    var filestoreRoot = Path.Combine(injectRoot, "filestore");
    var publicRoot = Path.Combine(filestoreRoot, "public");

    // resolve to full path so that the relative path cannot escape the filestore folder
    string rootedFilePath;
    bool isPublic;
    if (relativeFilePath == "index.js" || relativeFilePath == "index.css") {
      rootedFilePath = Path.Combine(injectRoot, relativeFilePath);
      isPublic = true;
    }
    else {
      try {
        rootedFilePath = Path.GetFullPath(Path.Combine(filestoreRoot, relativeFilePath));
      }
      catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
        return Results.BadRequest("Invalid file path.");
      }

      if (!Path.IsInFolder(filestoreRoot, rootedFilePath)) {
        return Results.NotFound();
      }

      isPublic = Path.IsInFolder(publicRoot, rootedFilePath);
    }

    // only allow unauthenticated access to certain public files
    // (like index.js and index.css which are needed for the inject feature to work at all),
    // but require authentication for all other files
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null && !isPublic) {
      return Results.Unauthorized();
    }

    if (!File.Exists(rootedFilePath)) {
      return Results.NotFound();
    }

    // check whether the user has access to the file unless it is a public file
    if (userInfo is not null && !isPublic) {
      var hasPermission = FileAccessInfo.CanAccessPath(rootedFilePath, userInfo, out var permissionHttpStatus);
      if (!hasPermission) {
        return Results.StatusCode(permissionHttpStatus);
      }
    }

    // serve the file with the correct content type
    var fileBytes = File.ReadAllBytes(rootedFilePath);
    if (!s_contentTypeProvider.TryGetContentType(rootedFilePath, out var contentType)) {
      contentType = "application/octet-stream";
    }
    return Results.Bytes(fileBytes, contentType);
  }
}
