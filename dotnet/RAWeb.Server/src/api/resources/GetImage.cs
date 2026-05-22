using Microsoft.Win32;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetImageEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/resources/image/{*image}", Handle);
    app.MapGet("/get-image.aspx", Handle);
  }

  private static IResult Handle(HttpContext ctx, string? image = null, string format = "png", string? frame = null, string theme = "light", string? fallback = null) {
    // get authentication information
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    if (string.IsNullOrEmpty(image)) {
      return Results.BadRequest("Missing image parameter.");
    }

    // process query parameters
    var imageFileName = image;
    format = format.ToLower();
    var frameMode = frame == "pc" ? "pc" : null;
    theme = theme == "dark" ? "dark" : "light";
    var fallbackImage = fallback ?? (image == "defaultwallpaper" ? "resource://static/lib/assets/wallpaper.png" : "resource://static/lib/assets/default.ico");

    // strip the App_Data/ prefix if present
    if (imageFileName.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase)) {
      imageFileName = imageFileName.Substring("App_Data/".Length);
    }

    // map special built-in image names to their embedded resource paths
    if (imageFileName == "defaultwallpaper") {
      imageFileName = theme == "dark"
          ? "resource://static/lib/assets/wallpaper-dark.png"
          : "resource://static/lib/assets/wallpaper.png";
    }
    else if (imageFileName == "defaulticon") {
      imageFileName = "resource://static/lib/assets/default.ico";
    }

    MemoryStream? imageStream;
    var sourceIsIcoFile = false;

    // if the image is from an exe/ico file, with the path provided from the registry,
    // read the image path from the registry and load the image as a bitmap image stream
    if (imageFileName.StartsWith("registry!") || imageFileName.StartsWith("registry:")) {
      var splitChar = imageFileName.Contains(':') ? ':' : '!';
      var appKeyName = imageFileName.Split(splitChar).Last();
      var maybeFileExtName = imageFileName.Split(splitChar)[1];
      if (maybeFileExtName == appKeyName) {
        maybeFileExtName = "";
      }

      imageStream = RegistryReader.ReadImageFromRegistry(appKeyName, maybeFileExtName, userInfo, httpContext: ctx);
    }

    // if the image is from a desktop is the registry, read the image path and serve it
    else if (imageFileName.StartsWith("registryDesktop!")) {
      // require centralized publishing to be enabled
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var centralizedPublishingCollectionName = AppId.ToCollectionName();
      if (!supportsCentralizedPublishing) {
        return Results.StatusCode(403);
      }

      // get the resource from the registry
      var desktopKeyName = imageFileName.Substring("registryDesktop!".Length);
      var resource = SystemDesktop.FromRegistry(centralizedPublishingCollectionName, desktopKeyName);
      if (resource is null) {
        return ServeDefaultIcon(404);
      }

      // check whether the user has access to the desktop
      var desktopRegistryKey = Registry.LocalMachine.OpenSubKey(
          $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{centralizedPublishingCollectionName}\RemoteDesktops\{desktopKeyName}"
      );
      var permissionHttpStatus = 500;
      if (desktopRegistryKey is null || !RegistryReader.CanAccessRemoteApp(desktopRegistryKey, userInfo, out permissionHttpStatus)) {
        return Results.StatusCode(permissionHttpStatus);
      }

      var _theme = theme == "dark" ? ManagedFileResource.ImageTheme.Dark : ManagedFileResource.ImageTheme.Light;
      try {
        // open the image file stream
        var wallpaperStream = ManagementServiceClient.Proxy.GetWallpaperStream(resource, _theme, userInfo.Sid);
        wallpaperStream.CopyTo(imageStream = new MemoryStream());
      }
      catch {
        return ServeDefaultIcon(500);
      }
    }

    // if the image is from a managed resource, serve it directly
    else if (imageFileName.StartsWith("managed-resources/")) {
      var managedResourceName = imageFileName.Substring("managed-resources/".Length);
      var rootedPath = Path.GetFullPath(Path.Combine(Constants.ManagedResourcesFolderPath, managedResourceName + ".resource"));

      // check whether the request is for a specific icon inside the managed resource
      // (e.g., managed-resources/resource.resource:iconname.png)
      // (e.g., managed-resources/resource.resource!iconname.png)
      var splitChar = imageFileName.Contains(':') ? ':' : '!';
      var splitParts = imageFileName.Split(splitChar);
      var maybeIconId = splitParts.Length == 2 ? splitParts[1] : null;

      // check whether the user has access to the managed resource file
      if (!FileAccessInfo.CanAccessPath(rootedPath, userInfo, out var permStatus)) {
        return Results.StatusCode(permStatus);
      }

      var _theme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
      try {
        imageStream = ImageUtilities.ImagePathToStream(rootedPath, maybeIconId, fallbackImage, _theme).ImageStream;
      }
      catch (ImageUtilities.UnsupportedImageFormatException) {
        return ServeDefaultIcon(400);
      }
      catch {
        return ServeDefaultIcon(500);
      }
    }

    // if the image is a resource embedded in the assembly, we need to extract it from there
    else if (imageFileName.StartsWith("resource://static/lib/assets/")) {
      var resourceName = imageFileName.Substring("resource://".Length);
      var assembly = System.Reflection.Assembly.GetExecutingAssembly();

      try {
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null) {
          return ServeDefaultIcon(404);
        }
        imageStream = new MemoryStream();
        resourceStream.CopyTo(imageStream);
      }
      catch {
        return ServeDefaultIcon(500);
      }
    }

    // otherwise, assume that the file name is a relative path to the image file
    else {
      try {
        imageStream = ReadImageFromFile(imageFileName, theme, fallbackImage, userInfo, out var fileExtension, out var permissionHttpStatus);

        if (permissionHttpStatus != 200) {
          return Results.StatusCode(permissionHttpStatus);
        }

        if (fileExtension == ".ico") {
          sourceIsIcoFile = true;
        }
      }
      catch {
        return ServeDefaultIcon(404);
      }
    }

    if (imageStream is null) {
      return ServeDefaultIcon(404);
    }

    // insert the image into a PC monitor frame
    if (frameMode == "pc") {
      // compose the desktop icon with the wallpaper and overlay
      var composed = ImageUtilities.ComposeDesktopIcon(imageStream, disposeOriginal: true);
      if (composed is null) {
        return Results.Problem("Error composing desktop icon.", statusCode: 500);
      }
      imageStream = composed;
    }

    // resize the image and serve it as an ico or png based on the requested format
    ImageUtilities.ImageDimensions outputDimensions;
    switch (format) {
      case "ico":
        // try to convert the image to ICO and serve it
        try {
          var iconStream = ImageUtilities.ImageToIco(imageStream);
          return ServeStream(iconStream, "image/x-icon");
        }
        catch {
          return ServeDefaultIcon();
        }

      case "png":
        // if the image was an ICO file, we need to convert it to PNG
        if (sourceIsIcoFile) {
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 256);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        }

        // otherwise, no resizing needed; serve original PNG
        break;

      case "png16":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 16);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      case "png32":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 32);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      case "png48":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 48);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      case "png64":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 64);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      case "png100":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 100);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      case "png256":
        outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 256);
        imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height, disposeOriginal: true);
        break;

      default:
        return Results.BadRequest("Invalid format specified.");
    }

    imageStream.Position = 0; // reset stream position if it was changed earlier

    return ServeStream(imageStream, "image/png");
  }

  private static IResult ServeStream(Stream stream, string mimeType) {
    // move stream position to beginning
    if (stream.CanSeek) {
      stream.Position = 0;
    }

    // build HTTP response
    var bytes = new MemoryStream();
    stream.CopyTo(bytes);
    return Results.Bytes(bytes.ToArray(), mimeType);
  }

  private static IResult ServeDefaultIcon(int statusCode = 200) {
    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    var defaultIconResourceName = ImageUtilities.DefaultIconPath.Replace("resource://", "");

    using var resourceStream = assembly.GetManifestResourceStream(defaultIconResourceName);
    if (resourceStream is null) {
      return Results.Problem("Default icon resource not found.", statusCode: 500);
    }

    var response = ImageUtilities.CreateResponse(resourceStream, (System.Net.HttpStatusCode)statusCode);
    var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
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

  private static MemoryStream? ReadImageFromFile(string imagePath, string theme, string fallbackImage, UserInformation userInfo, out string fileExtension, out int permissionHttpStatus) {
    // ensure paths are rooted
    var rootedImagePath = !Path.IsPathRooted(imagePath)
      ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, imagePath))
      : imagePath;
    var rootedFallbackPath = !string.IsNullOrEmpty(fallbackImage) && !Path.IsPathRooted(fallbackImage) && !fallbackImage.StartsWith("resource://")
      ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, fallbackImage))
      : fallbackImage;

    // strip extension so we can try both .png and .ico
    if (
      rootedImagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
      rootedImagePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
    ) {
      rootedImagePath = rootedImagePath.Substring(0, rootedImagePath.Length - 4);
    }

    // read the image into a memory stream
    var _theme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
    var imageResponse = ImageUtilities.ImagePathToStream(
      // prefer PNG format because they support larger image dimensions
      rootedImagePath + ".png",
      null,
      // fall back to ICO format if PNG not found, and use the fallback path if both not found
      ImageUtilities.ImagePathToStream(rootedImagePath + ".ico", null, rootedFallbackPath, _theme).ImagePath,
      _theme
    );

    fileExtension = Path.GetExtension(imageResponse.ImagePath).ToLower();

    // require the current user to have access to the image file
    var alwaysAllowedPaths = new string[] {
      Constants.AssetsFolderPath + "/wallpaper.png",
      Constants.AssetsFolderPath + "/wallpaper-dark.png",
      Constants.AssetsFolderPath + "/default.ico",
      Constants.AssetsFolderPath + "/desktop-frame.png",
    };

    var fileAlwaysAllowed = alwaysAllowedPaths.Contains(imageResponse.ImagePath, StringComparer.OrdinalIgnoreCase);
    permissionHttpStatus = 200;
    if (!fileAlwaysAllowed && !FileAccessInfo.CanAccessPath(imageResponse.ImagePath, userInfo, out permissionHttpStatus)) {
      return null;
    }

    return imageResponse.ImageStream;
  }
}
