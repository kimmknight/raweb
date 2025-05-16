using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

public partial class GetImage : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
		// Retrieve query parameters
		string imageFileName = Request.QueryString["image"];
		string format = Request.QueryString["format"] != null ? Request.QueryString["format"].ToLower() : null;
		string frame = Request.QueryString["frame"] == "pc" ? "pc" : null;
		string theme = Request.QueryString["theme"] == "dark" ? "dark" : "light";
		string fallbackImage = Request.QueryString["fallback"] != null ? Request.QueryString["fallback"] : "default.ico";

		if (string.IsNullOrEmpty(imageFileName) || string.IsNullOrEmpty(format))
		{
			Response.StatusCode = 400;
			Response.Write("Missing parameters.");
			return;
		}

		// try to find the image file path
		string imagePath = "";
		string fileExtension = Path.GetExtension(string.Format("{0}", imageFileName)).ToLower();
		if (theme == "dark")
		{
			// try to find the dark-themed image first
			string darkFileName = Path.GetDirectoryName(imageFileName);
			darkFileName += "\\" + Path.GetFileNameWithoutExtension(imageFileName) + "-dark" + fileExtension;
			FindImageFilePath(darkFileName, null, out imagePath, out fileExtension);
		}
		if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
		{
			// if dark-themed image not found, fallback to the original image
			FindImageFilePath(imageFileName, fallbackImage, out imagePath, out fileExtension);
		}

		// if the file extension and format are both ICO, serve the ICO directly
		if (fileExtension == ".ico" && format == "ico")
		{
			ServeImageAsIco(imagePath);
			return;
		}

		// insert the image into a PC monitor frame
		Stream imageStream = null;
		if (frame == "pc")
		{
			
			// Compose the desktop icon with the wallpaper and overlay
			imageStream = ComposeDesktopIcon(imagePath);
			if (imageStream == null)
			{
				Response.StatusCode = 500;
				Response.Write("Error composing desktop icon.");
				return;
			}
		}

		// read the image into a MemoryStream if a file is not already in it,
		// resize it (if needed), and serve it as PNG
		if (imageStream == null)
		{
			imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
		}
		Dimensions outputDimensions = GetImageDimensions(imageStream);
		switch (format)
		{
			case "ico": // source format is png, but requested format is ico
				if (outputDimensions.Width > 256 || outputDimensions.Height > 256)
				{
					outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 256);
					imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				}
				ConvertImageToIcoAndServe(imageStream);
				return;
			case "png":
				break; // no resizing needed; serve original PNG
			case "png16":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 16);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			case "png32":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 32);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			case "png48":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 48);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			case "png64":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 64);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			case "png100":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 100);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			case "png256":
				outputDimensions = GetResizedDimensionsFromMaxSize(imageStream, 256);
				imageStream = ResizeImage(imageStream, outputDimensions.Width, outputDimensions.Height);
				break;

			default:
				Response.StatusCode = 400;
				Response.Write("Invalid format specified.");
				return;
		}

		imageStream.Position = 0; // Reset stream position if it was set

		ServeStream(imageStream, "image/png");
	}

	private void FindImageFilePath(string imageFileName, string fallbackImage, out string imagePath, out string fileExtension)
	{
		// Initialize imagePath and determine file extension
		imagePath = Server.MapPath(string.Format("{0}", imageFileName));
		fileExtension = Path.GetExtension(imageFileName).ToLower();

		// If the file cannot be found (e.g., no extension is in the path)
		// attempt to find the file with .ico or .png extension
		if (!File.Exists(imagePath))
		{
			// check for .ico first
			imagePath = Server.MapPath(string.Format("{0}.ico", imageFileName));

			if (File.Exists(imagePath))
			{
				fileExtension = ".ico"; // Update fileExtension if ICO file exists
			}
			else
			{
				// If no .ico, check for .png
				imagePath = Server.MapPath(string.Format("{0}.png", imageFileName));
				if (File.Exists(imagePath))
				{
					fileExtension = ".png"; // Update fileExtension if PNG file exists
				}
			}
		}

		// If the image file doesn't exist, set to default image
		if (!File.Exists(imagePath) && !string.IsNullOrEmpty(fallbackImage))
		{
			imageFileName = fallbackImage;
			imagePath = Server.MapPath(imageFileName);
			fileExtension = ".png"; // Assume the default image is a PNG

			// If the default image also doesn't exist, return an error
			if (!File.Exists(imagePath))
			{
				Response.StatusCode = 404;
				Response.Write("Default image file not found.");
				return;
			}
		}
	}

	private void ConvertImageToIcoAndServe(Stream imageStream)
	{
		// Load the image stream as a Bitmap and convert to ICO
		using (Bitmap bitmap = new Bitmap(imageStream))
		{
			int width = bitmap.Width;
			int height = bitmap.Height;

			using (MemoryStream ms = new MemoryStream())
			{
				bitmap.Save(ms, ImageFormat.Png); // ICO supports PNG compression
				byte[] pngBytes = ms.ToArray();

				using (MemoryStream iconStream = new MemoryStream())
				{
					iconStream.Write(new byte[] { 0, 0, 1, 0, 1, 0, (byte)width, (byte)height, 0, 0, 0, 0, 32, 0 }, 0, 14); // set ico header, image metadata (22 bytes), 
					iconStream.Write(BitConverter.GetBytes(pngBytes.Length), 0, 4); // set image size in bytes
					iconStream.Write(BitConverter.GetBytes(22), 0, 4); // offset where to start writing image
					iconStream.Write(pngBytes, 0, pngBytes.Length); // write png data

					iconStream.Seek(0, SeekOrigin.Begin);
					using (Icon icon = new Icon(iconStream))
					{
						ServeStream(iconStream, "image/x-icon");
					}
				}
			}
		}
	}

	private void ServeImageAsIco(string imagePath)
	{
		// Serve the original ICO file directly
		using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
		{
			ServeStream(fileStream, "image/x-icon");
		}
	}


	private void ServeImageAsPng(string imagePath)
	{
		// Serve the existing PNG file
		using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
		{
			ServeStream(fileStream, "image/png");
		}
	}

	private MemoryStream ResizeImage(Stream stream, int width, int height)
	{
		MemoryStream outputStream = new MemoryStream();

		stream.Seek(0, SeekOrigin.Begin);
		using (Bitmap originalImage = new Bitmap(stream))
		{
			using (Bitmap resizedImage = new Bitmap(width, height))
			{
				using (Graphics graphics = Graphics.FromImage(resizedImage))
				{
					graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
					graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
					graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
					graphics.DrawImage(originalImage, 0, 0, width, height);
				}

				resizedImage.Save(outputStream, ImageFormat.Png);
			}
		}

		outputStream.Seek(0, SeekOrigin.Begin);
		return outputStream;
	}

	private MemoryStream ResizeImage(string imagePath, int width, int height)
	{
		// read the file into a MemoryStream
		using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
		{
			return ResizeImage(fileStream, width, height);
		}
	}

	private void ServeStream(Stream stream, string mimeType)
	{
		stream.Position = 0;
		Response.ContentType = mimeType;

		if (stream is MemoryStream)
		{
			Response.BinaryWrite(((MemoryStream)stream).ToArray());
		}
		else
		{
			using (MemoryStream ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				Response.BinaryWrite(ms.ToArray());
			}
		}

		stream.Dispose(); // dispose the stream after serving it
		Response.End();
	}

	private MemoryStream ComposeDesktopIcon(string wallpaperPath)
	{
		// the memory stream to return
		MemoryStream ms = new MemoryStream();

		// ensure the frame image exists
		string overlayPath = Server.MapPath("desktop-frame.png");
		if (!File.Exists(overlayPath))
		{
			Response.StatusCode = 404;
			Response.Write("PC frame overlay image not found.");
			ms.Dispose();
			return null;
		}

		// define target area and dimensions
		const int overlayWidth = 256;
        const int overlayHeight = 256;
        const int targetAreaWidth = 216;
        const int targetAreaHeight = 152;
        const int targetAreaX = 20;
        const int targetAreaY = 36;

		using (Image wallpaper = Image.FromFile(wallpaperPath))
        using (Image overlay = Image.FromFile(overlayPath))
        using (Bitmap resultImage = new Bitmap(overlayWidth, overlayHeight))
        using (Graphics graphics = Graphics.FromImage(resultImage))
		{
			// calculate the crop dimensions for the wallpaper
			// so that it fits the target area aspect ratio
			float aspectRatioWallpaper = (float)wallpaper.Width / wallpaper.Height;
            float aspectRatioTarget = (float)targetAreaWidth / targetAreaHeight;

			int cropX = 0;
            int cropY = 0;
            int cropWidth = wallpaper.Width;
            int cropHeight = wallpaper.Height;

			if (aspectRatioWallpaper > aspectRatioTarget)
			{
				// wallpaper is wider than target area
				cropWidth = (int)(wallpaper.Height * aspectRatioTarget);
				cropX = (wallpaper.Width - cropWidth) / 2;
			}
			else if (aspectRatioWallpaper < aspectRatioTarget)
			{
				// wallpaper is taller than target area
				cropHeight = (int)(wallpaper.Width / aspectRatioTarget);
				cropY = (wallpaper.Height - cropHeight) / 2;
			}

			// create a new bitmap with the cropped wallpaper section
            using (Bitmap croppedWallpaper = new Bitmap(cropWidth, cropHeight))
            using (Graphics croppedGraphics = Graphics.FromImage(croppedWallpaper))
            {
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

	private struct Dimensions
	{
		public int Width;
		public int Height;

		public Dimensions(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}

	private Dimensions GetImageDimensions(Stream imageStream)
	{
		int iconWidth = 0;
		int iconHeight = 0;
		using (var image = System.Drawing.Image.FromStream(imageStream, false, false))
		{       
			iconWidth = image.Width;
			iconHeight = image.Height;
		}
		return new Dimensions(iconWidth, iconHeight);
	}

	private Dimensions GetResizedDimensionsFromMaxSize(Stream imageStream, int maxSize)
	{
		// get the current dimensions of the image
		Dimensions originalDimensions = GetImageDimensions(imageStream);
		int iconWidth = originalDimensions.Width;
		int iconHeight = originalDimensions.Height;
		double aspectRatio = (double)iconWidth / iconHeight;

		// calculate the new dimensions that maintain the aspect ratio
		int newWidth = iconWidth;
		int newHeight = iconHeight;
		if (iconWidth > maxSize || iconHeight > maxSize)
		{
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

		return new Dimensions(newWidth, newHeight);
	}
}
