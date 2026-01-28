using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Microsoft.Win32;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class ResourceController : ApiController {
    [HttpGet]
    [Route("image/{*image}")]
    [Route("~/get-image.aspx")]
    [RequireAuthentication]
    public IHttpActionResult GetImage(string image, string format = "png", string frame = null, string theme = "light", string fallback = null) {
      // get authentication information
      var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);

      // process query parameters
      var imageFileName = image;
      format = format.ToLower();
      frame = frame == "pc" ? "pc" : null;
      theme = theme == "dark" ? "dark" : "light";
      var fallbackImage = fallback ?? (image == "defaultwallpaper" ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico");

      // if the image path starts with App_Data/, remove that part
      if (imageFileName.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase)) {
        imageFileName = imageFileName.Substring("App_Data/".Length);
      }

      if (string.IsNullOrEmpty(imageFileName) || string.IsNullOrEmpty(format)) {
        return BadRequest("Missing parameters.");
      }

      MemoryStream imageStream;
      var sourceIsIcoFile = false;

      // if the image is from an exe/ico file, with the path provided from the registry,
      // read the image path from the registry and load the image as a bitmap image stream
      var isRegistryImage = imageFileName.StartsWith("registry!") || imageFileName.StartsWith("registry:");
      if (isRegistryImage) {
        var splitChar = imageFileName.Contains(':') ? ':' : '!';
        var appKeyName = imageFileName.Split(splitChar).LastOrDefault();
        var maybeFileExtName = imageFileName.Split(splitChar)[1];
        if (maybeFileExtName == appKeyName) {
          maybeFileExtName = "";
        }

        imageStream = RegistryReader.ReadImageFromRegistry(appKeyName, maybeFileExtName, userInfo);
      }

      // if the image is from a desktop is the registry, read the image path and serve it
      else if (imageFileName.StartsWith("registryDesktop!")) {
        // require centralized publishing to be enabled
        var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
        var centralizedPublishingCollectionName = AppId.ToCollectionName();
        if (!supportsCentralizedPublishing) {
          return ResponseMessage(Request.CreateResponse(403));
        }

        // get the resource from the registry
        var desktopKeyName = imageFileName.Substring("registryDesktop!".Length);
        var resource = SystemDesktop.FromRegistry(centralizedPublishingCollectionName, desktopKeyName);
        if (resource == null) {
          return ServeDefaultIcon(HttpStatusCode.NotFound);
        }

        // check whether the user has access to the desktop
        var desktopRegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\CentralPublishedResources\\PublishedFarms\\" + centralizedPublishingCollectionName + "\\RemoteDesktops\\" + desktopKeyName);
        var hasPermission = RegistryReader.CanAccessRemoteApp(desktopRegistryKey, userInfo, out var permissionHttpStatus);
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse(permissionHttpStatus));
        }

        var _theme = theme == "dark" ? ManagedFileResource.ImageTheme.Dark : ManagedFileResource.ImageTheme.Light;
        try {
          // open the image file stream
          var wallpaperStream = SystemRemoteAppsClient.Proxy.GetWallpaperStream(resource, _theme, userInfo.Sid);
          wallpaperStream.CopyTo(imageStream = new MemoryStream());
        }
        catch (Exception) {
          return ServeDefaultIcon(HttpStatusCode.InternalServerError);
        }
      }

      // if the image is from a managed resource, serve it directly
      else if (imageFileName.StartsWith("managed-resources/")) {
        var managedResourceName = imageFileName.Substring("managed-resources/".Length);
        var rootedManagedResourcePath = Path.GetFullPath(Path.Combine(Constants.ManagedResourcesFolderPath, managedResourceName + ".resource"));

        // check whether the request is for a specific icon inside the managed resource
        // (e.g., managed-resources/resource.resource:iconname.png)
        // (e.g., managed-resources/resource.resource!iconname.png)
        var splitChar = imageFileName.Contains(':') ? ':' : '!';
        var splitParts = imageFileName.Split(splitChar);
        var maybeIconId = splitParts.Length == 2 ? splitParts[1] : null;

        // check whether the user has access to the managed resource file
        var hasPermission = FileAccessInfo.CanAccessPath(rootedManagedResourcePath, userInfo, out var permissionHttpStatus);
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse(permissionHttpStatus));
        }

        var _theme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
        try {
          imageStream = ImageUtilities.ImagePathToStream(rootedManagedResourcePath, maybeIconId, fallbackImage, _theme).ImageStream;
        }
        catch (ImageUtilities.UnsupportedImageFormatException) {
          return ServeDefaultIcon(HttpStatusCode.BadRequest);
        }
        catch {
          return ServeDefaultIcon(HttpStatusCode.InternalServerError);
        }
      }

      // otherwise, assume that the file name is a relative path to the image file
      else {
        try {
          imageStream = ReadImageFromFile(imageFileName, theme, fallbackImage, userInfo, out var fileExtension, out var permissionHttpStatus);

          if (permissionHttpStatus != 200) {
            return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
          }

          if (fileExtension == ".ico") {
            sourceIsIcoFile = true;
          }
        }
        catch {
          return ServeDefaultIcon(HttpStatusCode.NotFound);
        }

      }

      // insert the image into a PC monitor frame
      if (frame == "pc") {
        // compose the desktop icon with the wallpaper and overlay
        imageStream = ImageUtilities.ComposeDesktopIcon(imageStream, disposeOriginal: true);
        if (imageStream == null) {
          return InternalServerError(new Exception("Error composing desktop icon."));
        }
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
          catch (Exception) {
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
          return BadRequest("Invalid format specified.");
      }

      imageStream.Position = 0; // reset stream position if it was changed earlier

      return ServeStream(imageStream, "image/png");
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

    private IHttpActionResult ServeStream(Stream stream, string mimeType) {
      // move stream position to beginning
      if (stream.CanSeek) {
        stream.Position = 0;
      }

      // build HTTP response
      var response = new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new StreamContent(stream)
      };

      response.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

      // if length is known, set Content-Length
      if (stream.CanSeek) {
        response.Content.Headers.ContentLength = stream.Length;
      }

      return ResponseMessage(response);
    }

    /// <summary>
    /// Reads an image from a file path into a memory stream.
    /// Only PNG and ICO formats are supported.
    /// If a dark mode image is requested, and there is an image with the same name ending with -dark,
    /// that image will be used instead.
    /// </summary>
    private static MemoryStream ReadImageFromFile(string imagePath, string theme, string fallbackImage, UserInformation userInfo, out string fileExtension, out int permissionHttpStatus) {
      // ensure paths are rooted
      var rootedImagePath = !Path.IsPathRooted(imagePath) ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, imagePath)) : imagePath;
      var rootedFallbackPath = !string.IsNullOrEmpty(fallbackImage) ? !Path.IsPathRooted(fallbackImage) ? Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, fallbackImage)) : null : null;

      // remove PNG or ICO extension if present since they will be added later
      if (rootedImagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
        rootedImagePath = rootedImagePath.Substring(0, rootedImagePath.Length - 4);
      }
      else if (rootedImagePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) {
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
      var alwaysAllowedPaths = new string[]
      {
        Path.Combine(Constants.AssetsFolderPath, "wallpaper.png"),
        Path.Combine(Constants.AssetsFolderPath, "wallpaper-dark.png"),
        Path.Combine(Constants.AssetsFolderPath, "default.ico"),
        Path.Combine(Constants.AssetsFolderPath, "desktop-frame.png"),
      };
      var fileAlwaysAllowed = alwaysAllowedPaths.Contains(imageResponse.ImagePath, StringComparer.OrdinalIgnoreCase);
      permissionHttpStatus = 200;
      var hasPermission = fileAlwaysAllowed || FileAccessInfo.CanAccessPath(imageResponse.ImagePath, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return null;
      }

      return imageResponse.ImageStream;
    }

  }
}
