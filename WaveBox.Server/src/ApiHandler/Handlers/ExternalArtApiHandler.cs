using System;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model;
using WaveBox.Core;
using Ninject;
using WaveBox.Core.Model.Repository;
using System.IO;

namespace WaveBox.ApiHandler.Handlers
{
	class ExternalArtApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "externalart"; } }

		/// <summary>
		/// Process returns a file stream containing album art
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Check for the itemId
			if (!uri.Parameters.ContainsKey("id"))
			{
				processor.WriteErrorHeader();
				return;
			}

			// Convert to integer
			int itemId = 0;
			Int32.TryParse(uri.Parameters["id"], out itemId);

			// If art ID was invalid, write error header
			if (itemId == 0)
			{
				processor.WriteErrorHeader();
				return;
			}

			// Check for blur (value between 0 and 100)
			double blurSigma = 0;
			if (uri.Parameters.ContainsKey("blur"))
			{
				int blur = 0;
				Int32.TryParse(uri.Parameters["blur"], out blur);
				if (blur < 0)
					blur = 0;
				else if (blur > 100)
					blur = 100;

				blurSigma = (double)blur / 10.0;
			}

			IItemRepository itemRepository = Injection.Kernel.Get<IItemRepository>();
			ItemType type = itemRepository.ItemTypeForItemId(itemId);

			// Only support artist art right now
			if (type == ItemType.Artist || type == ItemType.AlbumArtist)
			{
				IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
				AlbumArtist artist = albumArtistRepository.AlbumArtistForId(itemId);

				// Grab the artist info from MusicBrainz
				MusicBrainz.Artist result = MusicBrainz.Artist.Query(artist.AlbumArtistName);

				processor.WriteText("MusicBrainz - id: " + result.Id, "text");

				/*

				// Grab art stream
				Art art = Injection.Kernel.Get<IArtRepository>().ArtForId(artId);
				Stream stream = CreateStream(art);

				// If the stream could not be produced, return error
				if ((object)stream == null)
				{
					processor.WriteErrorHeader();
					return;
				}

				// If art size requested...
				if (uri.Parameters.ContainsKey("size"))
				{
					int size = Int32.MaxValue;
					Int32.TryParse(uri.Parameters["size"], out size);

					// Parse size if valid
					if (size != Int32.MaxValue)
					{
						bool imageMagickFailed = false;
						if (ServerUtility.DetectOS() != ServerUtility.OS.Windows)
						{
							// First try ImageMagick
							try
							{
								Byte[] data = ResizeImageMagick(stream, size, blurSigma);
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
					lastModified = ((long)art.LastModified).ToDateTime();
				}
				processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), null, true, lastModified);

				// Close the file so we don't get sharing violations on future accesses
				stream.Close();*/
			}
			else
			{
				processor.WriteErrorHeader();
				return;
			}
		}

	}
}

