using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RAWeb.Server.Utilities;

public static class ImageUtilities {
  public static readonly string DefaultIconPath = Path.Combine(
  Constants.AssetsFolderPath,
  "default.ico"
);

  /// <summary>
  /// Converts an image path to a MemoryStream containing the image data.
  /// </summary>
  /// <param name="path"></param>
  /// <param name="index"></param>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static MemoryStream? ImagePathToStream(string path, int index = 0) {
    // check if the path is a valid absolute path that exists
    var isValidPath = Path.IsPathRooted(path) && File.Exists(path);
    if (!isValidPath) {
      return null;
    }

    // attempt to serve the icon from the specified path by extracting the embedded icon
    if (IsExeDllIco(path)) {
      try {
        // extract the icon handle
        var phiconLarge = new IntPtr[1];
        var result = ExtractIconEx(path, index, phiconLarge, null, 1);

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
        throw new Exception("Failed to extract icon from the specified path.");
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
        throw new Exception("Failed to read image file from the specified path.");
      }
    }

    return null;
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
}
