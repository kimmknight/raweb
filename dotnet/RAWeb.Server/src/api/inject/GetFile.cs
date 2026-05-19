using Microsoft.AspNetCore.StaticFiles;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetInjectFileEndpoint {

  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/inject/file/{*relativeFilePath}", Handle);
  }

  private static readonly FileExtensionContentTypeProvider s_contentTypeProvider = new();

  private static IResult Handle(string relativeFilePath, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    if (string.IsNullOrEmpty(relativeFilePath)) {
      return Results.BadRequest("Missing relative file path parameter.");
    }

    string rootedFilePath;
    if (relativeFilePath == "index.js" || relativeFilePath == "index.css") {
      rootedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", relativeFilePath);
    }
    else {
      rootedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", "filestore", relativeFilePath);
    }
    if (!File.Exists(rootedFilePath)) {
      return Results.NotFound();
    }

    // check whether the user has access to the file
    var hasPermission = FileAccessInfo.CanAccessPath(rootedFilePath, userInfo, out var permissionHttpStatus);
    if (!hasPermission) {
      return Results.StatusCode(permissionHttpStatus);
    }

    // serve the file with the correct content type
    var fileBytes = File.ReadAllBytes(rootedFilePath);
    if (!s_contentTypeProvider.TryGetContentType(rootedFilePath, out var contentType)) {
      contentType = "application/octet-stream";
    }
    return Results.Bytes(fileBytes, contentType);
  }
}
