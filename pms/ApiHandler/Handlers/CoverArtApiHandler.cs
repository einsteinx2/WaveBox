using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WaveBox.DataModel.Model;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class CoverArtApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public CoverArtApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			var artid = int.Parse(_uriW.getUriPart(2));
			var art = new CoverArt(artid);
			Image img = new Bitmap(art.artFile());

			if (_uriW.Parameters.ContainsKey("size"))
			{
				string size = null; 
				_uriW.Parameters.TryGetValue("size", out size);

				if (size != null)
				{
					img = resizeImage(img, new Size(int.Parse(size), int.Parse(size)));
				}
			}

			img.Save(_sh.socket.GetStream(), System.Drawing.Imaging.ImageFormat.Jpeg);

			Console.WriteLine("CoverArt: Not implemented yet.");
			_sh.outputStream.Write("CoverArt: Not implemented yet.");
		}

// Thanks to http://www.switchonthecode.com/tutorials/csharp-tutorial-image-editing-saving-cropping-and-resizing
		private static Image resizeImage(Image imgToResize, Size size)
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
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;

			g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
			g.Dispose();

			return (Image)b;
		}
	}
}
