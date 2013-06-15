using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WaveBox.Model;
using WaveBox.TcpServer.Http;
using WaveBox.Static;
using TagLib;
using System.Runtime.InteropServices;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
			Art art = new Art.Factory().CreateArt(artId);
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
					bool imageMagickFailed = false;
					if (Utility.DetectOS() != Utility.OS.Windows)
					{
						// First try ImageMagick
						try
						{
							logger.Info("Using Magick to resize image");
							Byte[] data = ResizeImageMagick(stream, size);
							stream = new MemoryStream(data, false);
						}
						catch
						{
							imageMagickFailed = true;
						}
					}

					// If ImageMagick dll isn't loaded, or this is Windows,  
					if (imageMagickFailed || Utility.DetectOS() == Utility.OS.Windows)
					{
						logger.Info("Using GDI to resize image");
						// Resize image, put it in memory stream
						Image resized = ResizeImageGDI(new Bitmap(stream), new Size(size, size));
						stream = new MemoryStream();
						resized.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
					}
				}
			}

			DateTime? lastModified = null;
			if (!ReferenceEquals(art.LastModified, null))
				lastModified = ((long)art.LastModified).ToDateTimeFromUnixTimestamp();
			Processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), null, true, lastModified);
			stream.Close();

			// Close the file so we don't get sharing violations on future accesses
			stream.Close();
		}


		private static byte[] ResizeImageMagick(Stream stream, int width)
		{
			// new wand
			IntPtr wand = ImageMagickInterop.NewWand();

			// get original image
			byte[] b = new byte[stream.Length];
			stream.Read(b, 0, (int)stream.Length);
			bool success = ImageMagickInterop.ReadImageBlob(wand, b);

			int sourceWidth = (int)ImageMagickInterop.GetWidth(wand);
			int sourceHeight = (int)ImageMagickInterop.GetHeight(wand);

			logger.Info(String.Format("sourceWidth: {0}, sourceHeight: {1}, stream length: {2}, success: {3}, wand: {4}", sourceWidth, sourceHeight, stream.Length, success, wand));
			
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

			logger.Info(String.Format("destWidth: {0}, destHeight: {1}", destWidth, destHeight));

			ImageMagickInterop.ResizeImage(wand, (IntPtr)destWidth, (IntPtr)destHeight, ImageMagickInterop.Filter.Lanczos, 1.0);
			byte[] newData = ImageMagickInterop.GetImageBlob(wand);

			logger.Info(String.Format("new wand size: width = {0}, height = {1}; newData len = {2}", ImageMagickInterop.GetWidth(wand), ImageMagickInterop.GetHeight(wand), newData.Length));

			// cleanup
			ImageMagickInterop.DestroyWand(wand);
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
