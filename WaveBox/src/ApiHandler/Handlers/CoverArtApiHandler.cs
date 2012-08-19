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

namespace WaveBox.ApiHandler.Handlers
{
	class CoverArtApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public CoverArtApiHandler(UriWrapper uri, IHttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process ()
		{
			// Check for the itemId
			if (!Uri.Parameters.ContainsKey ("id")) {
				Processor.WriteErrorHeader ();
				return;
			}

			// Convert to integer
			int itemId = Int32.MaxValue;
			Int32.TryParse (Uri.Parameters ["id"], out itemId);
			if (itemId == Int32.MaxValue) {
				Processor.WriteErrorHeader ();
				return;
			}

			// Get the item type
			ItemType type = Database.ItemTypeForItemId (itemId);

			// Send the appropriate art
			Stream stream = null;
			if (type == ItemType.Song) {
				stream = GetSongArt (new Song (itemId));
			} else if (type == ItemType.Folder) {
				stream = GetFolderArt (new Folder (itemId));
			} else if (type == ItemType.Album) {
				stream = GetAlbumArt (new Album (itemId));
			}

			if (stream == null) {
				Processor.WriteErrorHeader ();
				return;
			}

			if (Uri.Parameters.ContainsKey ("size")) {
				int size = Int32.MaxValue;
				Int32.TryParse (Uri.Parameters ["size"], out size);	
				if (size != Int32.MaxValue) {
					Image resized = ResizeImage (new Bitmap (stream), new Size (size, size));
					stream = new MemoryStream ();
					resized.Save (stream, System.Drawing.Imaging.ImageFormat.Jpeg);
				}
			}

			Processor.WriteFile(stream, 0, stream.Length);
		}

        private Stream GetSongArt(Song song)
        {
            var file = TagLib.File.Create(song.FilePath());
            string folderImagePath = null;

            if (Folder.ContainsImages(Path.GetDirectoryName(song.FilePath()), out folderImagePath))
            {
                return new FileStream(folderImagePath, FileMode.Open);
            }
            else if (file.Tag.Pictures.Length > 0)
            {
                return new MemoryStream(file.Tag.Pictures[0].Data.Data);
            } 

            return null;
        }

        private Stream GetAlbumArt(Album album)
        {
            return null;
        }

        private Stream GetFolderArt(Folder folder)
        {
            string imagePath = null;
            if (Folder.ContainsImages(folder.FolderPath, out imagePath))
            {
                return new FileStream(imagePath, FileMode.Open);
            }
            else
            {
                foreach(string file in Directory.GetFiles(folder.FolderPath))
                {
                    var tag = TagLib.File.Create(file);
                    if (tag.Tag.Pictures.Length > 0)
                    {
                        return new MemoryStream(tag.Tag.Pictures[0].Data.Data);
                    } 
                }
            }
            return null;
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
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.CompositingMode = CompositingMode.SourceCopy;

			g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
			g.Dispose();

			return (Image)b;
		}
	}
}
