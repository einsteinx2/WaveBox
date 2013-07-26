using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Ninject;
using TagLib;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model.Repository;
using WaveBox.Core;

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
			Art art = Injection.Kernel.Get<IArtRepository>().ArtForId(artId);
			Stream stream = CreateStream(art);

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
					if (ServerUtility.DetectOS() != ServerUtility.OS.Windows)
					{
						// First try ImageMagick
						try
						{
							Byte[] data = ResizeImageMagick(stream, size);
							stream = new MemoryStream(data, false);
						}
						catch
						{
							imageMagickFailed = true;
						}
					}

					// If ImageMagick dll isn't loaded, or this is Windows,
					if (imageMagickFailed || ServerUtility.DetectOS() == ServerUtility.OS.Windows)
					{
						// Resize image, put it in memory stream
						Image resized = ResizeImageGDI(new Bitmap(stream), new Size(size, size));
						stream = new MemoryStream();
						resized.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
					}
				}
			}

			DateTime? lastModified = null;
			if (!ReferenceEquals(art.LastModified, null))
			{
				lastModified = ((long)art.LastModified).ToDateTimeFromUnixTimestamp();
			}
			Processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), null, true, lastModified);

			// Close the file so we don't get sharing violations on future accesses
			stream.Close();
		}

		private byte[] ResizeImageMagick(Stream stream, int width)
		{
			// new wand
			IntPtr wand = ImageMagickInterop.NewWand();

			// get original image
			byte[] b = new byte[stream.Length];
			stream.Read(b, 0, (int)stream.Length);
			bool success = ImageMagickInterop.ReadImageBlob(wand, b);

			if (success)
			{
				int sourceWidth = (int)ImageMagickInterop.GetWidth(wand);
				int sourceHeight = (int)ImageMagickInterop.GetHeight(wand);

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

				ImageMagickInterop.ResizeImage(wand, (IntPtr)destWidth, (IntPtr)destHeight, ImageMagickInterop.Filter.Lanczos, 1.0);
				byte[] newData = ImageMagickInterop.GetImageBlob(wand);

				// cleanup
				ImageMagickInterop.DestroyWand(wand);
				return newData;
			}
			else
			{
				return b;
			}
		}

		// Thanks to http://www.switchonthecode.com/tutorials/csharp-tutorial-image-editing-saving-cropping-and-resizing
		/// <summary>
		/// Code which can resize an image and return it as requested
		/// </summary>
		private Image ResizeImageGDI(Image imgToResize, Size size)
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

		private Stream CreateStream(Art art)
		{
			if ((object)art.ArtId == null)
			{
				return null;
			}

			int? itemId = Injection.Kernel.Get<IArtRepository>().ItemIdForArtId((int)art.ArtId);

			if ((object)itemId == null)
			{
				return null;
			}

			ItemType type = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId((int)itemId);

			Stream stream = null;

			if (type == ItemType.Song)
			{
				stream = StreamForSong((int)itemId);
			}
			else if (type == ItemType.Folder)
			{
				stream = StreamForFolder((int)itemId);
			}

			return stream;
		}

		private Stream StreamForSong(int songId)
		{
			Song song = Injection.Kernel.Get<ISongRepository>().SongForId(songId);
			Stream stream = null;

			// Open the image from the tag
			TagLib.File f = null;
			try
			{
				f = TagLib.File.Create(song.FilePath());
				byte[] data = f.Tag.Pictures[0].Data.Data;

				stream = new MemoryStream(data);
			}
			catch (TagLib.CorruptFileException e)
			{
				if (logger.IsInfoEnabled) logger.Info(song.FileName + " has a corrupt tag so can't return the art. " + e);
			}
			catch (Exception e)
			{
				logger.Error("Error processing file: ", e);
			}

			return stream;
		}

		private Stream StreamForFolder(int folderId)
		{
			Folder folder = Injection.Kernel.Get<IFolderRepository>().FolderForId(folderId);
			Stream stream = null;

			string artPath = FolderArtPath(folder);

			if ((object)artPath != null)
			{
				stream = new FileStream(artPath, FileMode.Open, FileAccess.Read);
			}

			return stream;
		}

		private string FolderArtPath(Folder folder)
		{
			string artPath = null;

			foreach (string fileName in Injection.Kernel.Get<IServerSettings>().FolderArtNames)
			{
				string path = folder.FolderPath + Path.DirectorySeparatorChar + fileName;
				if (System.IO.File.Exists(path))
				{
					// Use this one
					artPath = path;
				}
			}

			if ((object)artPath == null)
			{
				// Check for any images
				FolderContainsImages(folder.FolderPath, out artPath);
			}

			return artPath;
		}

		private bool FolderContainsImages(string dir, out string firstImageFoundPath)
		{
			string[] validImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
			string ext = null;
			firstImageFoundPath = null;

			foreach (string file in Directory.GetFiles(dir))
			{
				ext = Path.GetExtension(file).ToLower();
				if (validImageExtensions.Contains(ext) && !Path.GetFileName(file).StartsWith("."))
				{
					firstImageFoundPath = file;
				}
			}

			// Return true if firstImageFoundPath exists
			return ((object)firstImageFoundPath != null);
		}
	}
}
