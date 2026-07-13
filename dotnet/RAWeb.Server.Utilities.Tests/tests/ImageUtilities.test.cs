using System.Drawing;
using System.Drawing.Imaging;

namespace RAWeb.Server.Utilities.Tests;

public class ImageUtilitiesTests {
  private static MemoryStream MakePngStream(int width, int height) {
    using var bmp = new Bitmap(width, height);
    var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    ms.Position = 0;
    return ms;
  }

  private static bool LooksLikePng(MemoryStream stream) {
    var pngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    var buffer = new byte[pngMagicBytes.Length];
    stream.Read(buffer, 0, buffer.Length);
    stream.Position = 0; // reset position after reading
    return buffer.SequenceEqual(pngMagicBytes);
  }

  private static bool LooksLikeJpeg(MemoryStream stream) {
    var jpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF };
    var buffer = new byte[jpegMagicBytes.Length];
    stream.Read(buffer, 0, buffer.Length);
    stream.Position = 0; // reset position after reading
    return buffer.SequenceEqual(jpegMagicBytes);
  }

  /// <summary>
  /// Creates a minimal ICO stream with a single 16x16 icon entry. This is used for testing
  /// the IsValidIcoStream method.
  /// The ICO format starts with a 6-byte header (ICONDIR) followed by one
  /// 16-byte ICONDIRENTRY.
  /// </summary>
  /// <returns></returns>
  private static MemoryStream MakeMinimalIcoStream(bool withoutImageData = false) {
    var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true);
    bw.Write((ushort)0);  // reserved
    bw.Write((ushort)1);  // type = 1 (icon)
    bw.Write((ushort)1);  // count = 1
    bw.Write((byte)16);   // width
    bw.Write((byte)16);   // height
    bw.Write((byte)0);    // color count
    bw.Write((byte)0);    // reserved2
    bw.Write((ushort)1);  // planes
    bw.Write((ushort)32); // bit count
    bw.Write((uint)4);    // size in bytes
    bw.Write((uint)22);   // offset (6 ICONDIR + 16 ICONDIRENTRY)
    if (!withoutImageData) {
      // Write 4 bytes of dummy image data to match the size specified in the header
      bw.Write(new byte[4]);
    }
    ms.Position = 0;
    return ms;
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsTrueForValidIcoBytes() {
    using var stream = MakeMinimalIcoStream();

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsTrue();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForInvalidIcoBytes() {
    using var stream = new MemoryStream([0x00, 0x00, 0x02, 0x00, 0x01, 0x00]);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForStreamSmallerOrLargerThanSpecifiedInIcoHeader() {
    // Create a valid ICO header but with no image data (size in header is 4 bytes)
    using var stream = MakeMinimalIcoStream(withoutImageData: true);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForJpegMagicBytes() {
    using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForPngMagicBytes() {
    using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForGifMagicBytes() {
    using var stream = new MemoryStream([0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForBmpMagicBytes() {
    using var stream = new MemoryStream([0x42, 0x4D]);

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForCursorIcoType() {
    // Create a valid ICO header but with an invalid type (should be 1 for icon)
    using var stream = MakeMinimalIcoStream();
    stream.Position = 2; // Move to the type field
    stream.WriteByte(0x02); // Change type to 2 (cursor)
    stream.Position = 0;

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task IsValidIcoStream_ReturnsFalseForEmptyStream() {
    using var stream = new MemoryStream();

    await Assert.That(ImageUtilities.IsValidIcoStream(stream)).IsFalse();
  }

  [Test]
  public async Task GetResizedDimensionsFromMaxSize_ReturnsSameDimensionsWhenUnderMaxSize() {
    using var stream = MakePngStream(32, 32);

    var dims = ImageUtilities.GetResizedDimensionsFromMaxSize(stream, 64);

    await Assert.That(dims.Width).IsEqualTo(32);
    await Assert.That(dims.Height).IsEqualTo(32);
  }

  [Test]
  public async Task GetResizedDimensionsFromMaxSize_ScalesDownWideImage() {
    using var stream = MakePngStream(200, 100);

    var dims = ImageUtilities.GetResizedDimensionsFromMaxSize(stream, 100);

    await Assert.That(dims.Width).IsEqualTo(100);
    await Assert.That(dims.Height).IsEqualTo(50);
  }

  [Test]
  public async Task GetResizedDimensionsFromMaxSize_ScalesDownTallImage() {
    using var stream = MakePngStream(100, 200);

    var dims = ImageUtilities.GetResizedDimensionsFromMaxSize(stream, 100);

    await Assert.That(dims.Width).IsEqualTo(50);
    await Assert.That(dims.Height).IsEqualTo(100);
  }

  [Test]
  public async Task GetResizedDimensionsFromMaxSize_ScalesDownSquareImage() {
    using var stream = MakePngStream(200, 200);

    var dims = ImageUtilities.GetResizedDimensionsFromMaxSize(stream, 100);

    await Assert.That(dims.Width).IsEqualTo(100);
    await Assert.That(dims.Height).IsEqualTo(100);
  }

  [Test]
  public async Task ImagePathToStream_ThrowsFileNotFoundWhenBothPathsAreNull() {
    await Assert.ThrowsAsync<FileNotFoundException>(() => Task.Run(() => {
      ImageUtilities.ImagePathToStream(null, null, null);
    }));
  }

  [Test]
  public async Task ImagePathToStream_UsesFallbackWhenPrimaryPathIsNull() {
    var tmp = Path.GetTempFileName() + ".png";
    try {
      using var src = MakePngStream(16, 16);
      File.WriteAllBytes(tmp, src.ToArray());

      var response = ImageUtilities.ImagePathToStream(null, null, tmp);

      await Assert.That(response.ImageStream).IsNotNull();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsPngStreamForValidPngFile() {
    var tmp = Path.GetTempFileName() + ".png";
    try {
      using var src = MakePngStream(32, 32);
      File.WriteAllBytes(tmp, src.ToArray());

      var response = ImageUtilities.ImagePathToStream(tmp);

      await Assert.That(response.ImageStream).IsNotNull();
      await Assert.That(response.ImageStream.Length).IsEqualTo(src.Length);

      var allResponseBytes = response.ImageStream.ToArray();
      var allSrcBytes = src.ToArray();
      await Assert.That(allResponseBytes).IsEquivalentTo(allSrcBytes);

      await Assert.That(LooksLikePng(response.ImageStream)).IsTrue();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ThrowsFileNotFoundForNonExistentPath() {
    await Assert.ThrowsAsync<FileNotFoundException>(() => Task.Run(() => {
      ImageUtilities.ImagePathToStream(@"C:\NonExistentPath\missing.png");
    }));
  }

  [Test]
  public async Task ImagePathToStream_ThrowsUnsupportedImageFormatForUnknownExtension() {
    var tmp = Path.GetTempFileName() + ".xyz";
    try {
      File.WriteAllBytes(tmp, []);

      await Assert.ThrowsAsync<ImageUtilities.UnsupportedImageFormatException>(() => Task.Run(() => {
        ImageUtilities.ImagePathToStream(tmp);
      }));
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task GetImageDimensions_ReturnsCorrectWidthAndHeight() {
    using var stream = MakePngStream(48, 72);

    var dims = ImageUtilities.GetImageDimensions(stream);

    await Assert.That(dims.Width).IsEqualTo(48);
    await Assert.That(dims.Height).IsEqualTo(72);
  }

  [Test]
  public async Task ResizeImage_OutputHasSpecifiedDimensions() {
    using var stream = MakePngStream(200, 100);

    using var resized = ImageUtilities.ResizeImage(stream, 50, 25);
    var dims = ImageUtilities.GetImageDimensions(resized);

    await Assert.That(dims.Width).IsEqualTo(50);
    await Assert.That(dims.Height).IsEqualTo(25);
  }

  [Test]
  public async Task ResizeImage_ReturnsValidPngStream() {
    using var stream = MakePngStream(64, 64);

    using var resized = ImageUtilities.ResizeImage(stream, 32, 32);

    await Assert.That(resized).IsNotNull();
    await Assert.That(resized.Length > 0).IsTrue();
    await Assert.That(LooksLikePng(resized)).IsTrue();
  }

  [Test]
  public async Task ImageToIco_ConvertsPngToValidIcoStream() {
    using var pngStream = MakePngStream(32, 32);
    var memStream = new MemoryStream(pngStream.ToArray());

    using var icoStream = ImageUtilities.ImageToIco(memStream);

    await Assert.That(ImageUtilities.IsValidIcoStream(icoStream)).IsTrue();
  }

  [Test]
  public async Task ImageToIco_ResizesOversizedImageBeforeConversion() {
    // 512x512 exceeds the 256x256 ICO maximum — should be resized down
    using var pngStream = MakePngStream(512, 512);
    var memStream = new MemoryStream(pngStream.ToArray());

    using var icoStream = ImageUtilities.ImageToIco(memStream);

    await Assert.That(ImageUtilities.IsValidIcoStream(icoStream)).IsTrue();
  }

  [Test]
  public async Task IsValidIcoFile_ReturnsTrueForValidIcoFile() {
    var tmp = Path.GetTempFileName() + ".ico";
    try {
      using var icoStream = MakeMinimalIcoStream();
      File.WriteAllBytes(tmp, icoStream.ToArray());

      var result = ImageUtilities.IsValidIcoFile(tmp);

      await Assert.That(result).IsTrue();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task IsValidIcoFile_ReturnsFalseForNonExistentPath() {
    var result = ImageUtilities.IsValidIcoFile(@"C:\NonExistentPath\missing.ico");

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task IsValidIcoFile_ReturnsFalseForNonIcoExtension() {
    var tmp = Path.GetTempFileName() + ".png";
    try {
      using var src = MakePngStream(16, 16);
      File.WriteAllBytes(tmp, src.ToArray());

      var result = ImageUtilities.IsValidIcoFile(tmp);

      await Assert.That(result).IsFalse();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ComposeDesktopIcon_ThrowsForNullStream() {
    await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Run(() => ImageUtilities.ComposeDesktopIcon(null!)));
  }

  [Test]
  public async Task ComposeDesktopIcon_ThrowsForDisposedStream() {
    var stream = MakePngStream(216, 152);
    stream.Dispose();

    await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Run(() => ImageUtilities.ComposeDesktopIcon(stream)));
  }

  [Test]
  public async Task CreateResponse_ReturnsImagePngContentType() {
    using var stream = MakePngStream(64, 64);

    using var response = ImageUtilities.CreateResponse(stream);

    await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("image/png");
  }

  [Test]
  public async Task CreateResponse_Returns200StatusCodeByDefault() {
    using var stream = MakePngStream(32, 32);

    using var response = ImageUtilities.CreateResponse(stream);

    await Assert.That((int)response.StatusCode).IsEqualTo(200);
  }

  [Test]
  public async Task CreateResponse_PreservesCustomStatusCode() {
    using var stream = MakePngStream(32, 32);

    using var response = ImageUtilities.CreateResponse(stream, System.Net.HttpStatusCode.NotFound);

    await Assert.That((int)response.StatusCode).IsEqualTo(404);
  }

  [Test]
  public async Task CreateResponse_SetsContentLengthHeader() {
    using var stream = MakePngStream(32, 32);

    using var response = ImageUtilities.CreateResponse(stream);

    await Assert.That(response.Content.Headers.ContentLength).IsNotNull();
    await Assert.That(response.Content.Headers.ContentLength > 0).IsTrue();
  }

  [Test]
  public async Task CreateResponse_ResizesImageLargerThan256() {
    using var stream = MakePngStream(512, 512);

    using var response = ImageUtilities.CreateResponse(stream);
    var content = await response.Content.ReadAsByteArrayAsync();
    using var resultStream = new MemoryStream(content);
    var dims = ImageUtilities.GetImageDimensions(resultStream);

    await Assert.That(dims.Width).IsEqualTo(256);
    await Assert.That(dims.Height).IsEqualTo(256);
  }

  [Test]
  public async Task ResizeImage_DisposesOriginalStreamWhenDisposeOriginalIsTrue() {
    var stream = MakePngStream(64, 64);

    using var resized = ImageUtilities.ResizeImage(stream, 32, 32, disposeOriginal: true);

    await Assert.That(stream.CanRead).IsFalse();
  }

  [Test]
  public async Task ImageToIco_ReturnsIcoDirectlyWhenInputIsAlreadyValidIco() {
    using var pngStream = MakePngStream(32, 32);
    using var firstIco = ImageUtilities.ImageToIco(new MemoryStream(pngStream.ToArray()));
    var icoBytes = firstIco.ToArray();

    using var secondIco = ImageUtilities.ImageToIco(new MemoryStream(icoBytes));

    await Assert.That(ImageUtilities.IsValidIcoStream(secondIco)).IsTrue();
  }

  [Test]
  public async Task ImageToIco_OutputIcoDirEntryHasCorrectWidthAndHeight() {
    using var pngStream = MakePngStream(48, 48);

    using var icoStream = ImageUtilities.ImageToIco(new MemoryStream(pngStream.ToArray()));
    var bytes = icoStream.ToArray();

    // ICONDIR is bytes 0-5, ICONDIRENTRY width is at byte 6, height at byte 7
    await Assert.That((int)bytes[6]).IsEqualTo(48);
    await Assert.That((int)bytes[7]).IsEqualTo(48);
  }

  [Test]
  public async Task GetHighResIcon_ThrowsForNonIcoPath() {
    await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
      ImageUtilities.GetHighResIcon(@"C:\Windows\explorer.exe")
    ));
  }

  [Test]
  public async Task ImagePathToStream_UsesFallbackWhenPrimaryPathInvalid() {
    var tmp = Path.GetTempFileName() + ".png";
    try {
      using var src = MakePngStream(16, 16);
      File.WriteAllBytes(tmp, src.ToArray());

      var response = ImageUtilities.ImagePathToStream(@"C:\NonExistent\missing.png", null, tmp);

      await Assert.That(response.ImageStream).IsNotNull();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsDarkThemeFileWhenDarkThemeRequested() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      Directory.CreateDirectory(tempDir);
      var lightPath = Path.Combine(tempDir, "icon.png");
      var darkPath = Path.Combine(tempDir, "icon-dark.png");
      using var lightSrc = MakePngStream(32, 32);
      using var darkSrc = MakePngStream(16, 16);
      File.WriteAllBytes(lightPath, lightSrc.ToArray());
      File.WriteAllBytes(darkPath, darkSrc.ToArray());

      var response = ImageUtilities.ImagePathToStream(lightPath, theme: ImageUtilities.ImageTheme.Dark);

      await Assert.That(response.ImagePath).IsEqualTo(darkPath);
    }
    finally {
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_LightThemeStripsDarkSuffixFromPath() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      Directory.CreateDirectory(tempDir);
      var lightPath = Path.Combine(tempDir, "icon.png");
      var darkPath = Path.Combine(tempDir, "icon-dark.png");
      using var lightSrc = MakePngStream(32, 32);
      using var darkSrc = MakePngStream(16, 16);
      File.WriteAllBytes(lightPath, lightSrc.ToArray());
      File.WriteAllBytes(darkPath, darkSrc.ToArray());

      var response = ImageUtilities.ImagePathToStream(darkPath, theme: ImageUtilities.ImageTheme.Light);

      await Assert.That(response.ImagePath).IsEqualTo(lightPath);
    }
    finally {
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsBytesForValidJpegFile() {
    var tmp = Path.GetTempFileName() + ".jpg";
    try {
      using var bmp = new Bitmap(32, 32);
      bmp.Save(tmp, ImageFormat.Jpeg);

      var response = ImageUtilities.ImagePathToStream(tmp);

      await Assert.That(response.ImageStream).IsNotNull();
      await Assert.That(response.ImageStream!.Length > 0).IsTrue();
      await Assert.That(LooksLikeJpeg(response.ImageStream)).IsTrue();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsIcoStreamForValidIcoFile() {
    var tmp = Path.GetTempFileName() + ".ico";
    try {
      using var pngStream = MakePngStream(32, 32);
      using var icoStream = ImageUtilities.ImageToIco(new MemoryStream(pngStream.ToArray()));
      File.WriteAllBytes(tmp, icoStream.ToArray());

      var response = ImageUtilities.ImagePathToStream(tmp);

      await Assert.That(response.ImageStream).IsNotNull();
      await Assert.That(ImageUtilities.IsValidIcoStream(response.ImageStream!)).IsTrue();
    }
    finally {
      if (File.Exists(tmp)) {
        File.Delete(tmp);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsStreamForExeWithIconIndex() {
    var response = ImageUtilities.ImagePathToStream(@"C:\Windows\explorer.exe", id: "0");

    await Assert.That(response.ImageStream).IsNotNull();
    await Assert.That(response.ImageStream!.Length > 0).IsTrue();
  }

  [Test]
  public async Task ImagePathToStream_ThrowsInvalidIndexExceptionForNonIntegerIdOnExe() {
    await Assert.ThrowsAsync<ImageUtilities.InvalidIndexException>(() => Task.Run(() => {
      ImageUtilities.ImagePathToStream(@"C:\Windows\explorer.exe", id: "notanumber");
    }));
  }

  [Test]
  public async Task ImagePathToStream_ReturnsPngFromManagedResourceFile() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      var resourcePath = Path.Combine(tempDir, "managed-resources", "test.resource");
      var resource = new Management.ManagedFileResource(resourcePath, "test", "remoteapplicationname:s:test", null, null, null, null);
      resource.WriteToFile();
      using var iconStream = MakePngStream(32, 32);
      resource.WriteImage(iconStream, "resource.png", Management.ManagedFileResource.ImageTheme.Light);

      var response = ImageUtilities.ImagePathToStream(resourcePath);

      await Assert.That(response.ImageStream).IsNotNull();
      await Assert.That(LooksLikePng(response.ImageStream!)).IsTrue();
    }
    finally {
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task ImagePathToStream_ReturnsDarkIconFromManagedResourceFile() {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      var resourcePath = Path.Combine(tempDir, "managed-resources", "test.resource");
      var resource = new Management.ManagedFileResource(resourcePath, "test", "remoteapplicationname:s:test", null, null, null, null);
      resource.WriteToFile();
      using var lightStream = MakePngStream(32, 32);
      resource.WriteImage(lightStream, "resource.png", Management.ManagedFileResource.ImageTheme.Light);
      using var darkStream = MakePngStream(16, 16);
      resource.WriteImage(darkStream, "resource.png", Management.ManagedFileResource.ImageTheme.Dark);

      var response = ImageUtilities.ImagePathToStream(resourcePath, theme: ImageUtilities.ImageTheme.Dark);

      await Assert.That(response.ImageStream).IsNotNull();
      await Assert.That(LooksLikePng(response.ImageStream!)).IsTrue();
    }
    finally {
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }
  }

  [Test]
  public async Task ComposeDesktopIcon_ReturnsValidPngStreamForValidWallpaper() {
    using var wallpaper = MakePngStream(1920, 1080);

    using var result = ImageUtilities.ComposeDesktopIcon(wallpaper);

    await Assert.That(result).IsNotNull();
    await Assert.That(LooksLikePng(result)).IsTrue();
  }

  [Test]
  public async Task ComposeDesktopIcon_DisposesOriginalStreamWhenDisposeOriginalIsTrue() {
    var wallpaper = MakePngStream(1920, 1080);

    using var result = ImageUtilities.ComposeDesktopIcon(wallpaper, disposeOriginal: true);

    await Assert.That(wallpaper.CanRead).IsFalse();
  }
}
