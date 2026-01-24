using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

public static class ImageUtilities {
  public static readonly string DefaultIconPath = Path.Combine(
  Constants.AssetsFolderPath,
  "default.ico"
);

  public class ImageResponse(string imagePath, MemoryStream? imageStream) {
    public string ImagePath = imagePath;
    public MemoryStream? ImageStream = imageStream;
  }

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
  /// <param name="id">For exe, dll, and ico files, the icon index. For .ico files, if it is null, the largest icon will be used. For .resource files, the file type association.</param>
  /// <param name="fallbackPath">If the specified path is invalid, attempts to use this path instead.</param>
  /// <param name="theme">For .resource files, the image theme to use (light or dark).</param>
  /// <returns></returns>
  /// <exception cref="FileNotFoundException">If the path and fallback path could not be found</exception>
  /// <exception cref="InvalidIndexException">For exe, dll, and, ico files: when the id cannot be converted to an integer</exception>
  /// <exception cref="UnsupportedImageFormatException"></exception>
  /// <exception cref="ImageParseFailureException"></exception>
  public static ImageResponse ImagePathToStream(string path, string? id = null, string? fallbackPath = null, ImageTheme? theme = ImageTheme.Light) {
    if (path is null && fallbackPath is not null) {
      path = fallbackPath;
      fallbackPath = null;
    }
    if (path is null) {
      throw new FileNotFoundException("The specified path and fallback path are both empty.");
    }

    // resolve the theme-specific path
    if (!IsManagedResourcePath(path) && theme == ImageTheme.Dark) {
      path = ToDarkPath(path, ToLightPath(path));
    }
    else {
      path = ToLightPath(path);
    }

    // check if the path is a valid absolute path that exists
    var isValidPath = Path.IsPathRooted(path) && File.Exists(path);

    // if the path is invalid, try to resolve the path for the fallback icon
    if (!isValidPath && fallbackPath is not null) {
      if (!IsManagedResourcePath(fallbackPath) && theme == ImageTheme.Dark) {
        path = ToDarkPath(fallbackPath, ToLightPath(fallbackPath));
      }
      else {
        path = ToLightPath(fallbackPath);
      }
      isValidPath = Path.IsPathRooted(path) && File.Exists(path);
    }

    // if the path is still invalid, raise an error
    if (!isValidPath) {
      throw new FileNotFoundException("The specified path is invalid.");
    }

    // for managed resource files, extract the image directly
    if (IsManagedResourcePath(path)) {
      try {
        var managedResource = ManagedFileResource.FromResourceFile(path);
        var _theme = theme == ImageTheme.Light ? ManagedFileResource.ImageTheme.Light : ManagedFileResource.ImageTheme.Dark;
        return new ImageResponse(path, managedResource.ReadImageStream(out _, _theme, id));
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

    // if the image is an ico file and we do not need a specific index,
    // use the highest-resolution icon from the .ico file
    if (isIco(path) && id is null) {
      try {
        var imageStream = GetHighResIcon(path);
        return new ImageResponse(path, imageStream);
      }
      catch {
        throw new ImageParseFailureException();
      }
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

        return new ImageResponse(path, imageStream);
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
          return new ImageResponse(path, new MemoryStream(data));
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

  public static ImageResponse ImagePathToStream(string path, int id, string? fallbackPath = null) {
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
  public static MemoryStream ResizeImage(Stream stream, int width, int height, bool disposeOriginal = false) {
    var outputStream = new MemoryStream();

    stream.Seek(0, SeekOrigin.Begin);
    using (var originalImage = new Bitmap(stream)) {

      using (var resizedImage = new Bitmap(width, height)) {
        using (var graphics = Graphics.FromImage(resizedImage)) {
          graphics.CompositingQuality = CompositingQuality.HighQuality;
          graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
          graphics.SmoothingMode = SmoothingMode.HighQuality;
          graphics.DrawImage(originalImage, 0, 0, width, height);
        }

        resizedImage.Save(outputStream, ImageFormat.Png);
        if (disposeOriginal) {
          stream.Dispose(); // we no longer need the original image stream
        }
      }
    }

    outputStream.Seek(0, SeekOrigin.Begin);
    return outputStream;
  }

  /// <summary>
  /// Composes a desktop icon by overlaying the provided wallpaper stream
  /// </summary>
  /// <param name="wallpaperStream"></param>
  /// <param name="disposeOriginal">Before returning the desktop icon, dispose the original wallpaper stream</param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="FileNotFoundException"></exception>
  public static MemoryStream ComposeDesktopIcon(Stream wallpaperStream, bool disposeOriginal = false) {
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
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.None;
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

    if (disposeOriginal) {
      wallpaperStream.Dispose();
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

  [DllImport("user32.dll", CharSet = CharSet.Auto)]
  static extern uint PrivateExtractIcons(
    string lpszFile,
    int nIconIndex,
    int cxIcon,
    int cyIcon,
    IntPtr[] phicon,
    int[]? piconid,
    int nIcons,
    int flags
  );

  [DllImport("user32.dll")]
  public static extern int DestroyIcon(IntPtr hIcon);

  public sealed class IcoImages(string filePath, int width, int height, int index, uint byteSize, uint offset) {
    public string FilePath = filePath;
    public int Width = width;
    public int Height = height;
    public int Index = index;
    public uint ByteSize = byteSize;
    public uint Offset = offset;

    public override string ToString() {
      return $"Index: {Index}, Size: {Width}x{Height}, ByteSize: {ByteSize}, Offset: {Offset}";
    }

    /// <summary>
    /// Converts the icon to a standalone ICO image stream.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public MemoryStream ToIco(string? overrideFilePath = null) {
      using var pngStream = ToPng(overrideFilePath);
      return PngBytesToIco(pngStream.ToArray(), Width, Height);
    }

    /// <summary>
    /// Converts the icon to a standalone PNG image stream.
    /// </summary>
    /// <param name="overrideFilePath"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public MemoryStream ToPng(string? overrideFilePath = null) {
      // read the image starting at the offset for the largest icon
      using (var fs = new FileStream(overrideFilePath ?? FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        fs.Seek(Offset, SeekOrigin.Begin);

        // read the raw icon data
        var iconData = new byte[ByteSize];
        var bytesRead = fs.Read(iconData, 0, (int)ByteSize);

        if (bytesRead != ByteSize) {
          throw new Exception("Failed to read the complete icon data.");
        }

        return new MemoryStream(iconData);
      }
    }
  }

  /// <summary>
  /// Gets all icon sizes contained within the specified .ico file.
  /// </summary>
  /// <param name="filePath"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  private static List<IcoImages> GetAllIconSizes(string filePath) {
    // require the file to be an .ico file
    if (!Path.GetExtension(filePath).Equals(".ico", StringComparison.CurrentCultureIgnoreCase)) {
      throw new InvalidOperationException("The specified file is not an icon file.");
    }

    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
      return GetAllIconSizes(fs, filePath);
  }

  /// <summary>
  /// Gets all icon sizes contained within the specified .ico image stream.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  private static List<IcoImages> GetAllIconSizes(Stream imageStream, string filePath = "") {
    List<IcoImages> sizes = [];

    // reset stream position
    imageStream.Seek(0, SeekOrigin.Begin);

    using (var br = new BinaryReader(imageStream, System.Text.Encoding.UTF8, leaveOpen: true)) {
      // ICONDIR structure (6 bytes)
      var reserved = br.ReadUInt16(); // reserved (must be 0)
      var type = br.ReadUInt16();     // resource type (1 for icon)
      var count = br.ReadUInt16();    // number of images in the file

      if (reserved != 0 || type != 1) {
        throw new InvalidOperationException("The file is not a valid icon file.");
      }

      // read each ICONDIRENTRY structure (16 bytes for each)
      for (var i = 0; i < count; i++) {
        var width = br.ReadByte();
        var height = br.ReadByte();
        var colorCount = br.ReadByte(); // color count (0 for 256+ colors)
        var reserved2 = br.ReadByte();  // reserved (must be 0)
        var planes = br.ReadUInt16(); // color planes
        var bitCount = br.ReadUInt16(); // bits per pixel
        var sizeInBytes = br.ReadUInt32(); // size of the image data in bytes
        var offset = br.ReadUInt32();   // offset of the image data from the file start

        if (reserved2 != 0) {
          throw new InvalidOperationException("The file is not a valid icon file.");
        }

        // width or height of 0 means 256 pixels
        var w = width == 0 ? 256 : width;
        var h = height == 0 ? 256 : height;

        sizes.Add(new IcoImages(filePath, w, h, i, sizeInBytes, offset));
      }
    }

    return sizes;
  }

  /// <summary>
  /// Determines if the specified .ico file contains valid icon entries.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  public static bool IsValidIcoFile(string path) {
    try {
      var sizes = GetAllIconSizes(path);
      return sizes.Count > 0;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  /// Determines if the specified image stream contains valid icon entries
  /// from an .ico file.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <returns></returns>
  public static bool IsValidIcoStream(Stream imageStream) {
    try {
      var sizes = GetAllIconSizes(imageStream);
      return sizes.Count > 0;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  /// Extracts the highest resolution icon from the specified file path.
  /// 
  /// This can only be used on .ico files.
  /// <br /><br />
  /// See <see cref="IShellItemImageFactory.GetImage(ShellItemImageSize, ShellItemImageFactoryFlags, out nint)" />.
  /// </summary>
  /// <param name="path"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <returns></returns>
  public static MemoryStream? GetHighResIcon(string path) {
    // require the file to be an .ico file
    if (!Path.GetExtension(path).Equals(".ico", StringComparison.CurrentCultureIgnoreCase)) {
      throw new InvalidOperationException("The specified file is not an icon file.");
    }

    // find the available icon sizes
    var sizes = GetAllIconSizes(path);
    if (sizes.Count == 0) {
      throw new InvalidOperationException("The specified icon file does not contain any icons.");
    }

    // get the largest available icon
    return sizes.OrderByDescending(s => s.Width * s.Height).First().ToIco();
  }

  /// <summary>
  /// Converts PNG byte array to ICO format in a MemoryStream.
  /// </summary>
  /// <param name="pngBytes"></param>
  /// <param name="width"></param>
  /// <param name="height"></param>
  /// <returns></returns>
  private static MemoryStream PngBytesToIco(byte[] pngBytes, int width, int height) {
    var icoStream = new MemoryStream();
    using (var bw = new BinaryWriter(icoStream, System.Text.Encoding.UTF8, true)) {
      // ICONDIR
      bw.Write((ushort)0); // reserved
      bw.Write((ushort)1); // type (1 for icon)
      bw.Write((ushort)1); // number of images (only 1 PNG icon)

      // ICONDIRENTRY
      bw.Write((byte)(width >= 256 ? 0 : width));   // width
      bw.Write((byte)(height >= 256 ? 0 : height)); // height
      bw.Write((byte)0);                            // number of colors in the palette (0 for no palette/RGBA)
      bw.Write((byte)0);                            // reserved
      bw.Write((ushort)1);                          // color planes
      bw.Write((ushort)32);                         // bits per pixel
      bw.Write((uint)pngBytes.Length);              // byte size of the image data
      bw.Write((uint)(6 + 16));                     // offset for image (number of bytes in ICONDIR + ICONDIRENTRY)

      // image data
      bw.Write(pngBytes);
    }

    icoStream.Position = 0;
    return icoStream;
  }

  /// <summary>
  /// Converts a image stream to ICO format in a MemoryStream.
  /// The ICO will contain a single PNG-compressed icon.
  /// <br /><br />
  /// If the image is larger than 256x256, it will be resized to
  /// fit within those dimensions. Resizing maintains the original aspect ratio.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <returns></returns>
  public static MemoryStream ImageToIco(MemoryStream imageStream) {
    var inputImageDimensions = GetImageDimensions(imageStream);

    // resize the image if it is larger than 256x256 pixels
    // because ICO only supports sizes up to 256x256
    if (inputImageDimensions.Width > 256 || inputImageDimensions.Height > 256) {
      var outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 256);
      imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
    }


    // if the image is already an ICO, return it directly
    if (IsValidIcoStream(imageStream)) {
      return imageStream;
    }

    // load the image stream as a Bitmap and convert to ICO
    using (var bitmap = new Bitmap(imageStream)) {
      var width = bitmap.Width;
      var height = bitmap.Height;

      // extract the bytes of the bitmap as a PNG
      using var ms = new MemoryStream();
      bitmap.Save(ms, ImageFormat.Png);
      var pngBytes = ms.ToArray();

      return PngBytesToIco(pngBytes, width, height);
    }
  }

  /// <summary>
  /// Deletes a GDI object. Use this to delete bitmap handles obtained from IShellItemImageFactory.
  /// </summary>
  /// <param name="hObject"></param>
  /// <returns></returns>
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObject);

  private static bool isIco(string path) {
    return Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
  }

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

  /// <summary>
  /// Removes -dark from the file path to get the light theme version.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  private static string ToLightPath(string path, string? fallbackPath = null) {
    if (IsManagedResourcePath(path) || (IsExeDllIco(path) && !isIco(path))) {
      return path; // only .ico and other supported image formats support themed paths
    }

    var directory = Path.GetDirectoryName(path);
    var filename = Path.GetFileNameWithoutExtension(path);
    var extension = Path.GetExtension(path);

    // remove the -dark suffix if present
    if (filename.EndsWith("-dark", StringComparison.OrdinalIgnoreCase)) {
      filename = filename.Substring(0, filename.Length - 5);
    }

    // check for a matching light theme file
    var themedImagePath = FindImageFilePath(directory, filename, extension);
    if (themedImagePath is not null && File.Exists(themedImagePath)) {
      return themedImagePath;
    }

    return fallbackPath ?? path;
  }

  /// <summary>
  /// Adds -dark to the file path to get the dark theme version.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  private static string ToDarkPath(string path, string? fallbackPath = null) {
    if (IsManagedResourcePath(path) || (IsExeDllIco(path) && !isIco(path))) {
      return path; // only .ico and other supported image formats support themed paths
    }

    var directory = Path.GetDirectoryName(path);
    var filename = Path.GetFileNameWithoutExtension(path);
    var extension = Path.GetExtension(path);

    // add the -dark suffix if not already present
    if (!filename.EndsWith("-dark", StringComparison.OrdinalIgnoreCase)) {
      filename += "-dark";
    }

    // check for a matching dark theme file
    var themedImagePath = FindImageFilePath(directory, filename, extension);
    if (themedImagePath is not null && File.Exists(themedImagePath)) {
      return themedImagePath;
    }

    return fallbackPath ?? path;
  }

  private static string? FindImageFilePath(string? directory, string imageFileName, string? extension) {
    // if there is no directory, use the app data folder
    var root = string.IsNullOrEmpty(directory) ? Constants.AppDataFolderPath : directory;

    // check if the image with the specified extension exists
    string imagePath;
    if (!string.IsNullOrEmpty(extension)) {
      imagePath = Path.Combine(root, string.Format("{0}{1}", imageFileName, extension));
      if (File.Exists(imagePath)) {
        return imagePath;
      }
    }

    // otherwise, check for .ico first
    imagePath = Path.Combine(root, string.Format("{0}.ico", imageFileName));
    if (File.Exists(imagePath)) {
      return imagePath;
    }
    else {
      // If no .ico, check for .png
      imagePath = Path.Combine(root, string.Format("{0}.png", imageFileName));
      if (File.Exists(imagePath)) {
        return imagePath;
      }
    }

    return null;
  }
}
