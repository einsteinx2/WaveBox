using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WaveBox.DataModel.Model;
using WaveBox.Http;
using WaveBox.DataModel.Singletons;
using TagLib;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for ArtApiHandler class
		/// </summary>
		public ArtApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a file stream containing album art
		/// </summary>
		public void Process()
		{
			// Check for the itemId
			if (!Uri.Parameters.ContainsKey("id"))
			{
				Processor.WriteErrorHeader();
				return;
			}

			// Convert to integer
			int artId = Int32.MaxValue;
			Int32.TryParse(Uri.Parameters["id"], out artId);
			
			// If art ID was invalid, write error header
			if (artId == Int32.MaxValue)
			{
				Processor.WriteErrorHeader();
				return;
			}

			// Grab art stream
			Art art = new Art(artId);
			Stream stream = art.Stream;


			// If the stream could not be produced, return error
			if ((object)stream == null)
			{
				Processor.WriteErrorHeader();
				return;
			}

			// If art size requested...
			if (Uri.Parameters.ContainsKey("size"))
			{
				int size = Int32.MaxValue;
				Int32.TryParse(Uri.Parameters["size"], out size);

				// Parse size if valid
				if (size != Int32.MaxValue)
				{
					if (WaveBoxService.DetectOS() == WaveBoxService.OS.Windows)
					{
						Console.WriteLine("Using GDI to resize image");
						// Resize image, put it in memory stream
						Image resized = ResizeImageGDI(new Bitmap(stream), new Size(size, size));
						stream = new MemoryStream();
						resized.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
					}
					else 
					{
						Console.WriteLine("Using Magick to resize image");
						Byte[] data = ResizeImageMagick(stream, size);
						stream = new MemoryStream(data, false);
					}
				}
			}

			// Write file to HTTP response
            var dict = new Dictionary<string, string>();
            Processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), dict, true);
            stream.Close();

            // Close the file so we don't get sharing violations on future accesses
            stream.Close();
		}


		private static byte[] ResizeImageMagick(Stream stream, int width)
		{
			ImageMagickInterop.WandGenesis();

			// new wand
			IntPtr wand = ImageMagickInterop.NewWand();

			// get original image
			byte[] b = new byte[stream.Length];
			stream.Read(b, 0, (int)stream.Length);
			bool success = ImageMagickInterop.ReadImageBlob(wand, b);

			int sourceWidth = (int)ImageMagickInterop.GetWidth(wand);
			int sourceHeight = (int)ImageMagickInterop.GetHeight(wand);

			Console.WriteLine("sourceWidth: {0}, sourceHeight: {1}, stream length: {2}, success: {3}, wand: {4}", sourceWidth, sourceHeight, stream.Length, success, wand);
			
			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;
			
			nPercentW = ((float)width / (float)sourceWidth);
			nPercentH = ((float)width / (float)sourceHeight);
			
			if (nPercentH < nPercentW)
			{
				nPercent = nPercentH;
			}
			else
			{
				nPercent = nPercentW;
			}
			
			int destWidth = (int)(sourceWidth * nPercent);
			int destHeight = (int)(sourceHeight * nPercent);

			Console.WriteLine("destWidth: {0}, destHeight: {1}", destWidth, destHeight);

			ImageMagickInterop.ResizeImage(wand, (IntPtr)destWidth, (IntPtr)destHeight, ImageMagickInterop.Filter.Lanczos, 1.0);
			byte[] newData = ImageMagickInterop.GetImageBlob(wand);

			Console.WriteLine("new wand size: width = {0}, height = {1}; newData len = {2}", ImageMagickInterop.GetWidth(wand), ImageMagickInterop.GetHeight(wand), newData.Length);

			// cleanup
			ImageMagickInterop.DestroyWand(wand);
			ImageMagickInterop.WandTerminus();
			return newData;
		}

		// Thanks to http://www.switchonthecode.com/tutorials/csharp-tutorial-image-editing-saving-cropping-and-resizing
		/// <summary>
		/// Code which can resize an image and return it as requested
		/// </summary>
		private static Image ResizeImageGDI(Image imgToResize, Size size)
		{
			int sourceWidth = imgToResize.Width;
			int sourceHeight = imgToResize.Height;

			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;

			nPercentW = ((float)size.Width / (float)sourceWidth);
			nPercentH = ((float)size.Height / (float)sourceHeight);

			if (nPercentH < nPercentW)
			{
				nPercent = nPercentH;
			}
			else
			{
				nPercent = nPercentW;
			}

			int destWidth = (int)(sourceWidth * nPercent);
			int destHeight = (int)(sourceHeight * nPercent);

			Bitmap b = new Bitmap(destWidth, destHeight);
			Graphics g = Graphics.FromImage((Image)b);
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;

			g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
			g.Dispose();

			return (Image)b;
		}
	}
}
