using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
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

      Stream imageStream;
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
        int permissionHttpStatus;
        var desktopRegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\CentralPublishedResources\\PublishedFarms\\" + centralizedPublishingCollectionName + "\\RemoteDesktops\\" + desktopKeyName);
        var hasPermission = RegistryReader.CanAccessRemoteApp(desktopRegistryKey, userInfo, out permissionHttpStatus);
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
        int permissionHttpStatus;
        var hasPermission = FileAccessInfo.CanAccessPath(rootedManagedResourcePath, userInfo, out permissionHttpStatus);
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse(permissionHttpStatus));
        }

        var _theme = theme == "dark" ? ImageUtilities.ImageTheme.Dark : ImageUtilities.ImageTheme.Light;
        try {
          imageStream = ImageUtilities.ImagePathToStream(rootedManagedResourcePath, maybeIconId, fallbackImage, _theme);
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
        string fileExtension;
        int permissionHttpStatus;
        imageStream = ReadImageFromFile(imageFileName, theme, fallbackImage, userInfo, out fileExtension, out permissionHttpStatus);

        if (permissionHttpStatus != 200) {
          return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
        }

        if (fileExtension == ".ico") {
          sourceIsIcoFile = true;
        }
      }

      // insert the image into a PC monitor frame
      if (frame == "pc") {
        // compose the desktop icon with the wallpaper and overlay
        var newImageStream = ImageUtilities.ComposeDesktopIcon(imageStream);
        imageStream.Dispose(); // we no longer need the original image stream
        imageStream = newImageStream;
        if (newImageStream == null) {
          return InternalServerError(new Exception("Error composing desktop icon."));
        }
      }

      // resize the image and serve it as an ico or png based on the requested format
      var outputDimensions = ImageUtilities.GetImageDimensions(imageStream);
      switch (format) {
        case "ico":
          // resize the image if it is larger than 256x256 pixels
          // because our ICO implementation supports only 256x256 max size
          if (outputDimensions.Width > 256 || outputDimensions.Height > 256) {
            outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 256);
            imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          }

          // try to convert the image to ICO and serve it
          try {
            return ConvertImageToIcoAndServe(imageStream);
          }
          catch (System.Runtime.InteropServices.ExternalException) {
            // some icons throw "A generic error occurred in GDI+."
            return ServeDefaultIcon();
          }
        case "png":
          // if the image was an ICO file, we need to convert it to PNG
          if (sourceIsIcoFile) {
            outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 256);
            imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          }

          // otherwise, no resizing needed; serve original PNG
          break;
        case "png16":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 16);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          break;

        case "png32":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 32);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          break;

        case "png48":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 48);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          break;

        case "png64":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 64);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          break;

        case "png100":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 100);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
          break;

        case "png256":
          outputDimensions = ImageUtilities.GetResizedDimensionsFromMaxSize(imageStream, 256);
          imageStream = ImageUtilities.ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
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

    private IHttpActionResult ServeImageAsIco(string imagePath) {
      using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read)) {
        return ServeStream(fileStream, "image/x-icon");
      }
    }

    private IHttpActionResult ConvertImageToIcoAndServe(Stream imageStream) {
      // Load the image stream as a Bitmap and convert to ICO
      using (var bitmap = new Bitmap(imageStream)) {
        imageStream.Dispose(); // we no longer need the original image stream

        var width = bitmap.Width;
        var height = bitmap.Height;

        using (var ms = new MemoryStream()) {
          bitmap.Save(ms, ImageFormat.Png); // ICO supports PNG compression
          var pngBytes = ms.ToArray();

          var iconStream = new MemoryStream();
          iconStream.Write(new byte[] { 0, 0, 1, 0, 1, 0, (byte)width, (byte)height, 0, 0, 0, 0, 32, 0 }, 0, 14); // set ico header, image metadata (22 bytes), 
          iconStream.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4); // set image size in bytes
          iconStream.Write(BitConverter.GetBytes(22), 0, 4); // offset where to start writing image
          iconStream.Write(pngBytes, 0, pngBytes.Length); // write png data

          iconStream.Seek(0, SeekOrigin.Begin);
          return ServeStream(iconStream, "image/x-icon");
        }
      }
    }

    private static FileStream ReadImageFromFile(string imageFileName, string theme, string fallbackImage, UserInformation userInfo, out string fileExtension, out int permissionHttpStatus) {
      // try to find the image file path
      string imagePath = null;
      fileExtension = Path.GetExtension(string.Format("{0}", imageFileName)).ToLower();
      if (theme == "dark") {
        // try to find the dark-themed image first
        var darkFileName = Path.GetDirectoryName(imageFileName);
        darkFileName += "\\" + Path.GetFileNameWithoutExtension(imageFileName) + "-dark" + fileExtension;
        FindImageFilePath(darkFileName, null, out imagePath, out fileExtension);
      }
      if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath) || !FileAccessInfo.CanAccessPath(imagePath, userInfo)) {
        // if dark-themed image not found or access is denied, fallback to the original image (or the fallback image)
        FindImageFilePath(imageFileName, fallbackImage, out imagePath, out fileExtension);
      }

      // require the current user to have access to the image file
      var alwaysAllowedPaths = new string[]
      {
        Path.GetFullPath(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "lib/assets/wallpaper.png")),
        Path.GetFullPath(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "lib/assets/wallpaper-dark.png")),
        Path.GetFullPath(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "lib/assets/default.ico")),
        Path.GetFullPath(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "lib/assets/desktop-frame.png")),
      };
      var fileAlwaysAllowed = alwaysAllowedPaths.Contains(Path.GetFullPath(imagePath), StringComparer.OrdinalIgnoreCase);
      permissionHttpStatus = 200;
      var hasPermission = fileAlwaysAllowed || FileAccessInfo.CanAccessPath(imagePath, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return null;
      }

      return new FileStream(imagePath, FileMode.Open, FileAccess.Read);
    }

    private static void FindImageFilePath(string imageFileName, string fallbackImage, out string imagePath, out string fileExtension) {
      // Initialize imagePath and determine file extension
      var root = Constants.AppDataFolderPath;
      imagePath = Path.Combine(root, string.Format("{0}", imageFileName));
      fileExtension = Path.GetExtension(imageFileName).ToLower();

      // If the file cannot be found (e.g., no extension is in the path)
      // attempt to find the file with .ico or .png extension
      if (!File.Exists(imagePath)) {
        // check for .ico first
        imagePath = Path.Combine(root, string.Format("{0}.ico", imageFileName));

        if (File.Exists(imagePath)) {
          fileExtension = ".ico"; // Update fileExtension if ICO file exists
        }
        else {
          // If no .ico, check for .png
          imagePath = Path.Combine(root, string.Format("{0}.png", imageFileName));
          if (File.Exists(imagePath)) {
            fileExtension = ".png"; // Update fileExtension if PNG file exists
          }
        }
      }

      // If the image file doesn't exist, set to default image
      if (!File.Exists(imagePath) && !string.IsNullOrEmpty(fallbackImage)) {
        imageFileName = fallbackImage;
        imagePath = Path.Combine(root, imageFileName);
        fileExtension = ".png"; // Assume the default image is a PNG

        // If the default image also doesn't exist, throw an error
        if (!File.Exists(imagePath)) {
          throw new FileNotFoundException("Default image file not found.", imageFileName);
        }
      }
    }
  }
}
