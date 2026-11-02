using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities.Tests;

public class SystemDesktopWallpaperTests {
  [Test]
  public async Task ResizeWallpaper_ShrinksLargeImagesTo800Pixels() {
    using var bitmap = new System.Drawing.Bitmap(1600, 900);
    var input = new MemoryStream();
    bitmap.Save(input, System.Drawing.Imaging.ImageFormat.Png);
    input.Position = 0;

    using var result = SystemDesktop.ResizeWallpaper(input);
    using var image = System.Drawing.Image.FromStream(result);

    await Assert.That(image.Width).IsEqualTo(800);
    await Assert.That(image.Height).IsEqualTo(450);
  }

  /// <summary>
  /// The management service pipe client falls back to the system wallpaper when this throws.
  /// </summary>
  [Test]
  public async Task ResizeWallpaper_ThrowsArgumentExceptionForInvalidImage() {
    var notAnImage = new MemoryStream("this is not an image"u8.ToArray());

    await Assert.That(() => SystemDesktop.ResizeWallpaper(notAnImage)).Throws<ArgumentException>();
  }
}
