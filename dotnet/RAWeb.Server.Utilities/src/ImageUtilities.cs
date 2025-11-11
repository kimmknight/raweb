using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

public static class ImageUtilities {
  public static readonly string DefaultIconPath = Path.Combine(
  Constants.AssetsFolderPath,
  "default.ico"
);

  /// <summary>
  /// Converts an image path to a MemoryStream containing the image data.
  /// <br /><br />
  /// This method DOES NOT verify if the current user can access the specified path.
  /// It is the caller's responsibility to determine when it is appropriate to
  /// check access permissions and ensure that the user has the necessary permissions
  /// to read the image at the specified path.
  /// See example below:
  /// <code>
  /// var hasPermission = FileAccessInfo.CanAccessPath(iconPath, authenticatedUserInfo);
  /// </code>
  /// </summary>
  /// <param name="path"></param>
  /// <param name="id">For exe, dll, and ico files, the icon index. For .resource files, the file type association.</param>
  /// <param name="fallbackPath">If the specified path is invalid, attempts to use this path instead.</param>
  /// <param name="theme">For .resource files, the image theme to use (light or dark).</param>
  /// <returns></returns>
  /// <exception cref="FileNotFoundException">If the path and fallback path could not be found</exception>
  /// <exception cref="InvalidIndexException">For exe, dll, and, ico files: when the id cannot be converted to an integer</exception>
  /// <exception cref="UnsupportedImageFormatException"></exception>
  /// <exception cref="ImageParseFailureException"></exception>
  public static MemoryStream? ImagePathToStream(string path, string? id = null, string? fallbackPath = null, ImageTheme? theme = ImageTheme.Light) {
    // check if the path is a valid absolute path that exists
    var isValidPath = Path.IsPathRooted(path) && File.Exists(path);

    // if the path is invalid, try to resolve the path for the fallback icon
    if (!isValidPath && fallbackPath is not null) {
      path = fallbackPath;
      isValidPath = Path.IsPathRooted(path) && File.Exists(path);
    }

    // if the path is still invalid, raise an error
    if (!isValidPath) {
      throw new FileNotFoundException("The specified path is invalid.");
    }

    // attempt to serve the icon from the specified path by extracting the embedded icon
    if (IsExeDllIco(path)) {
      if (!int.TryParse(id ?? "0", out var iconIndex)) {
        throw new InvalidIndexException();
      }

      try {
        // extract the icon handle
        var phiconLarge = new IntPtr[1];
        var result = ExtractIconEx(path, iconIndex, phiconLarge, null, 1);

        // if the result is UINT_MAX, the path or index is invalid
        if (result == uint.MaxValue) {
          var errorCode = Marshal.GetLastWin32Error();
          throw new Win32Exception(errorCode);
        }

        // convert the icon handle to an Icon object and save it to a MemoryStream
        var iconLarge = Icon.FromHandle(phiconLarge[0]);
        var imageStream = new MemoryStream();
        iconLarge.ToBitmap().Save(imageStream, ImageFormat.Png);
        imageStream.Position = 0;

        // dispose the icon and handle
        DestroyIcon(phiconLarge[0]);
        iconLarge.Dispose();

        return imageStream;
      }
      catch {
        throw new ImageParseFailureException();
      }
    }

    // for managed resource files, extract the image directly
    if (IsManagedResourcePath(path)) {
      try {
        var managedResource = ManagedFileResource.FromResourceFile(path);
        var _theme = theme == ImageTheme.Light ? ManagedFileResource.ImageTheme.Light : ManagedFileResource.ImageTheme.Dark;
        return managedResource.ReadImageStream(out _, _theme, id);
      }
      catch (FileNotFoundException) {
        if (fallbackPath is not null) {
          return ImagePathToStream(fallbackPath, id, null, theme);
        }
        else {
          throw;
        }
      }
      catch {
        throw new ImageParseFailureException();
      }
    }

    // for other file types, attempt to serve the image file directly
    if (IsSupportedImageFormat(path)) {
      try {
        using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
          var data = new byte[fileStream.Length];
          fileStream.Read(data, 0, data.Length);
          return new MemoryStream(data);
        }
      }
      catch {
        throw new ImageParseFailureException();
      }
    }

    throw new UnsupportedImageFormatException();
  }

  public class UnsupportedImageFormatException : Exception {
    public UnsupportedImageFormatException() : base("The specified file type is not supported.") {
    }
  }

  public class ImageParseFailureException : Exception {
    public ImageParseFailureException() : base("Failed to read image file from the specified path.") {
    }
  }

  public class InvalidIndexException : Exception {
    public InvalidIndexException() : base("The specified icon index is invalid.") {
    }
  }

  public static MemoryStream? ImagePathToStream(string path, int id = 0, string? fallbackPath = null) {
    return ImagePathToStream(path, id.ToString(), fallbackPath);
  }

  public enum ImageTheme {
    Light,
    Dark
  }

  /// <summary>
  /// Resizes an image from the provided stream to the specified width and height.
  /// </summary>
  /// <param name="stream"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <returns></returns>
  public static MemoryStream ResizeImage(Stream stream, int width, int height) {
    var outputStream = new MemoryStream();

    stream.Seek(0, SeekOrigin.Begin);
    using (var originalImage = new Bitmap(stream)) {

      using (var resizedImage = new Bitmap(width, height)) {
        using (var graphics = Graphics.FromImage(resizedImage)) {
          graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
          graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
          graphics.DrawImage(originalImage, 0, 0, width, height);
        }

        resizedImage.Save(outputStream, ImageFormat.Png);
        stream.Dispose(); // we no longer need the original image stream
      }
    }

    outputStream.Seek(0, SeekOrigin.Begin);
    return outputStream;
  }

  public static MemoryStream ComposeDesktopIcon(Stream wallpaperStream) {
    if (wallpaperStream == null || !wallpaperStream.CanRead) {
      throw new ArgumentNullException("Invalid wallpaper stream.");
    }

    // the memory stream to return
    var ms = new MemoryStream();

    // ensure the frame image exists
    var overlayPath = Path.Combine(Constants.AssetsFolderPath, "desktop-frame.png");
    if (!File.Exists(overlayPath)) {
      ms.Dispose();
      throw new FileNotFoundException("PC frame overlay image file not found.", overlayPath);
    }

    // define target area and dimensions
    const int overlayWidth = 256;
    const int overlayHeight = 256;
    const int targetAreaWidth = 216;
    const int targetAreaHeight = 152;
    const int targetAreaX = 20;
    const int targetAreaY = 36;

    using (var wallpaper = Image.FromStream(wallpaperStream))
    using (var overlay = Image.FromFile(overlayPath))
    using (var resultImage = new Bitmap(overlayWidth, overlayHeight))
    using (var graphics = Graphics.FromImage(resultImage)) {
      // calculate the crop dimensions for the wallpaper
      // so that it fits the target area aspect ratio
      var aspectRatioWallpaper = (float)wallpaper.Width / wallpaper.Height;
      var aspectRatioTarget = (float)targetAreaWidth / targetAreaHeight;

      var cropX = 0;
      var cropY = 0;
      var cropWidth = wallpaper.Width;
      var cropHeight = wallpaper.Height;

      if (aspectRatioWallpaper > aspectRatioTarget) {
        // wallpaper is wider than target area
        cropWidth = (int)(wallpaper.Height * aspectRatioTarget);
        cropX = (wallpaper.Width - cropWidth) / 2;
      }
      else if (aspectRatioWallpaper < aspectRatioTarget) {
        // wallpaper is taller than target area
        cropHeight = (int)(wallpaper.Width / aspectRatioTarget);
        cropY = (wallpaper.Height - cropHeight) / 2;
      }

      // create a new bitmap with the cropped wallpaper section
      using (var croppedWallpaper = new Bitmap(cropWidth, cropHeight))
      using (var croppedGraphics = Graphics.FromImage(croppedWallpaper)) {
        croppedGraphics.DrawImage(wallpaper, new Rectangle(0, 0, cropWidth, cropHeight), cropX, cropY, cropWidth, cropHeight, GraphicsUnit.Pixel);

        // draw the resized and cropped wallpaper onto the result image
        graphics.DrawImage(
          croppedWallpaper,
          new Rectangle(targetAreaX, targetAreaY, targetAreaWidth, targetAreaHeight),
          new Rectangle(0, 0, cropWidth, cropHeight),
          GraphicsUnit.Pixel);
      }

      // overlay the result image with the PC frame
      graphics.DrawImage(overlay, 0, 0, overlayWidth, overlayHeight);

      // save the result to the MemoryStream
      resultImage.Save(ms, ImageFormat.Png);

      // rewind the stream so it can be read from the beginning
      ms.Seek(0, SeekOrigin.Begin);
    }

    return ms;
  }

  public struct ImageDimensions(int width, int height) {
    public int Width = width;
    public int Height = height;
  }

  /// <summary>
  /// Gets the dimensions of the image from the provided stream.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <returns></returns>
  public static ImageDimensions GetImageDimensions(Stream imageStream) {
    var iconWidth = 0;
    var iconHeight = 0;
    using (var image = Image.FromStream(imageStream, false, false)) {
      iconWidth = image.Width;
      iconHeight = image.Height;
    }
    return new ImageDimensions(iconWidth, iconHeight);
  }

  /// <summary>
  /// Calculates the dimensions for the image once it is resized to fit within the specified max size.
  /// This method creates dimensions that maintain the original aspect ratio of the image.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <param name="maxSize"></param>
  /// <returns></returns>
  public static ImageDimensions GetResizedDimensionsFromMaxSize(Stream imageStream, int maxSize) {
    // get the current dimensions of the image
    var originalDimensions = GetImageDimensions(imageStream);
    var iconWidth = originalDimensions.Width;
    var iconHeight = originalDimensions.Height;
    var aspectRatio = (double)iconWidth / iconHeight;

    // calculate the new dimensions that maintain the aspect ratio
    var newWidth = iconWidth;
    var newHeight = iconHeight;
    if (iconWidth > maxSize || iconHeight > maxSize) {
      if (aspectRatio > 1) // width is greater than height
      {
        newWidth = maxSize;
        newHeight = (int)(maxSize / aspectRatio);
      }
      else // height is greater than or equal to width
      {
        newHeight = maxSize;
        newWidth = (int)(maxSize * aspectRatio);
      }
    }

    return new ImageDimensions(newWidth, newHeight);
  }

  /// <summary>
  /// Creates a response fopr serving a PNG image stream.
  /// If the icon is larger than 256x256, it is resized.
  /// <br /><br  />
  /// From the Web API controller context, this can be used as:
  /// <code>
  /// var response = ImageUtilities.CreateResponse(iconStream);
  /// return ResponseMessage(response);
  /// </code>
  /// </summary>
  /// <param name="icon"></param>
  /// <returns></returns>
  public static System.Net.Http.HttpResponseMessage CreateResponse(Stream icon, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) {
    var outputDimensions = GetResizedDimensionsFromMaxSize(icon, 256);
    var stream = ResizeImage(icon, outputDimensions.Width, outputDimensions.Height);

    // build HTTP response
    var response = new System.Net.Http.HttpResponseMessage(statusCode) {
      Content = new System.Net.Http.StreamContent(stream)
    };

    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

    // if length is known, set Content-Length
    if (stream.CanSeek) {
      response.Content.Headers.ContentLength = stream.Length;
    }

    return response;
  }

  [DllImport("shell32.dll", CharSet = CharSet.Auto)]
  public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[]? phiconSmall, [In] uint nIcons);

  [DllImport("user32.dll")]
  public static extern int DestroyIcon(IntPtr hIcon);

  private static bool IsExeDllIco(string path) {
    return Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
      || Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
      || Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsSupportedImageFormat(string path) {
    var ext = Path.GetExtension(path).ToLower();
    return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif";
  }

  private static bool IsManagedResourcePath(string path) {
    if (string.IsNullOrEmpty(path)) {
      return false;
    }

    var ext = Path.GetExtension(path).ToLower();
    if (ext != ".resource") {
      return false;
    }

    var pathParts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
    if (pathParts.Length < 2) {
      return false;
    }

    // the last part should be the .resource file name
    // and the preceding part should be "managed-resources"
    return pathParts[^2].Equals("managed-resources", StringComparison.OrdinalIgnoreCase);
  }
}
