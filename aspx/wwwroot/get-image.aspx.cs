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

		if (string.IsNullOrEmpty(imageFileName) || string.IsNullOrEmpty(format))
		{
			Response.StatusCode = 400;
			Response.Write("Missing parameters.");
			return;
		}

		// Initialize imagePath and determine file extension
		string imagePath = Server.MapPath(string.Format("{0}", imageFileName));
		string fileExtension = Path.GetExtension(imageFileName).ToLower();

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
		if (!File.Exists(imagePath))
		{
			imageFileName = "default.ico";
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


		// Now, handle the format logic based on the found file's extension
		if (fileExtension == ".ico" && format == "ico")
		{
			// If the file is an ICO and the requested format is ICO, serve the original ICO file
			ServeImageAsIco(imagePath);
		}
		else
		{
			// Otherwise, handle other formats (convert, resize, or serve PNG)
			switch (format)
			{
				case "ico":
					// Convert the image to ICO if it's not already an ICO
					if (fileExtension != ".ico")
					{
						ConvertImageToIco(imagePath);
					}
					else
					{
						ServeImageAsIco(imagePath); // Redundant but explicit, in case it's still ICO
					}
					break;

				case "png":
					ServeImageAsPng(imagePath);
					break;

				case "png32":
					ServeImageAsResizedPng(imagePath, 32, 32);
					break;

				default:
					Response.StatusCode = 400;
					Response.Write("Invalid format specified.");
					break;
			}
		}
	}

	private void ConvertImageToIco(string imagePath)
	{
		// Load the image as a Bitmap and convert to ICO
		using (Bitmap bitmap = new Bitmap(imagePath))
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

	private void ServeImageAsResizedPng(string imagePath, int width, int height)
	{
		// Load and resize the PNG file
		using (Bitmap originalImage = new Bitmap(imagePath))
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

				using (MemoryStream pngStream = new MemoryStream())
				{
					resizedImage.Save(pngStream, ImageFormat.Png);
					ServeStream(pngStream, "image/png");
				}
			}
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

		Response.End();
	}
}
