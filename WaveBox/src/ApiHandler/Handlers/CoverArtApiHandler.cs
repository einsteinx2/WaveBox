using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WaveBox.DataModel.Model;
using WaveBox.HttpServer;

namespace WaveBox.ApiHandler.Handlers
{
	class CoverArtApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public CoverArtApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			var artid = int.Parse(Uri.UriPart(2));
			var art = new CoverArt(artid);
			Image img = new Bitmap(art.ArtFile());

			if (Uri.Parameters.ContainsKey("size"))
			{
				string size = null; 
				Uri.Parameters.TryGetValue("size", out size);

				if (size != null)
				{
					img = ResizeImage(img, new Size(int.Parse(size), int.Parse(size)));
				}
			}

			img.Save(Processor.Socket.GetStream(), System.Drawing.Imaging.ImageFormat.Jpeg);

			Console.WriteLine("[COVERARTAPI] Not implemented yet.");
			Processor.OutputStream.Write("[COVERARTAPI] Not implemented yet.");
		}

// Thanks to http://www.switchonthecode.com/tutorials/csharp-tutorial-image-editing-saving-cropping-and-resizing
		private static Image ResizeImage(Image imgToResize, Size size)
		{
			int sourceWidth = imgToResize.Width;
			int sourceHeight = imgToResize.Height;

			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;

			nPercentW = ((float)size.Width / (float)sourceWidth);
			nPercentH = ((float)size.Height / (float)sourceHeight);

			if (nPercentH < nPercentW)
				nPercent = nPercentH;
			else
				nPercent = nPercentW;

			int destWidth = (int)(sourceWidth * nPercent);
			int destHeight = (int)(sourceHeight * nPercent);

			Bitmap b = new Bitmap(destWidth, destHeight);
			Graphics g = Graphics.FromImage((Image)b);
			g.InterpolationMode = InterpolationMode.High;

			g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
			g.Dispose();

			return (Image)b;
		}
	}
}
