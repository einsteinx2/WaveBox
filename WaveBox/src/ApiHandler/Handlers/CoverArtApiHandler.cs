using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WaveBox.DataModel.Model;
using WaveBox.HttpServer;
using TagLib;

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
            Image img = null;

            if (Uri.Parameters.ContainsKey("song"))
            {
                string songIdString = null;
                Uri.Parameters.TryGetValue("song", out songIdString);

                if (songIdString != null)
                {
                    int songId = Int32.MaxValue;
                    Int32.TryParse(songIdString, out songId);

                    if (songId != Int32.MaxValue) 
                    {
                        var stream = GetSongArt(new Song(songId));
                        if(stream != null)
                            img = new Bitmap(stream);
                    }
                }
            }
            else
            if (Uri.Parameters.ContainsKey("folder"))
            {
                string folderIdString = null;
                Uri.Parameters.TryGetValue("folder", out folderIdString);
                if (folderIdString != null)
                {
                    int folderId = Int32.MaxValue;
                    Int32.TryParse(folderIdString, out folderId);

                    if (folderId != Int32.MaxValue)
                    {
                        var stream = GetFolderArt(new Folder(folderId));
                        if(stream != null)
                            img = new Bitmap(stream);
                    }

                }
            }
            else
            if (Uri.Parameters.ContainsKey("album"))
            {
                string albumIdString = null;
                Uri.Parameters.TryGetValue("album", out albumIdString);
                if (albumIdString != null)
                {
                    int albumId = Int32.MaxValue;
                    Int32.TryParse(albumIdString, out albumId);

                    if (albumId != Int32.MaxValue)
                        img = new Bitmap(GetAlbumArt(new Album(albumId)));
                }
            }
            else
            {
                var artid = int.Parse(Uri.UriPart(2));
                var art = new CoverArt(artid);
                var ms = new MemoryStream(System.IO.File.ReadAllBytes(art.ArtFile()));
                img = new Bitmap(ms);
            }

            if(img == null) return;

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

        private Bitmap GetSongArt(Song song)
        {
            var file = TagLib.File.Create(song.FilePath());
            string folderImagePath = null;

            if (Folder.ContainsImages(Path.GetDirectoryName(song.FilePath()), out folderImagePath))
            {
                return new Bitmap(folderImagePath);
            }

            else if (file.Tag.Pictures.Length > 0)
            {
                return new Bitmap(new MemoryStream(file.Tag.Pictures[0].Data.Data));
            } 

            return null;
        }

        private Bitmap GetAlbumArt(Album album)
        {
            return null;
        }

        private Bitmap GetFolderArt(Folder folder)
        {
            string imagePath = null;
            if (Folder.ContainsImages(folder.FolderPath, out imagePath))
            {
                return new Bitmap(imagePath);
            }
            else
            {
                foreach(string file in Directory.GetFiles(folder.FolderPath))
                {
                    var tag = TagLib.File.Create(file);
                    if (tag.Tag.Pictures.Length > 0)
                    {
                        return new Bitmap(new MemoryStream(tag.Tag.Pictures[0].Data.Data));
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
