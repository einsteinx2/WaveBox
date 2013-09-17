using System;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model;
using WaveBox.Core;
using Ninject;
using WaveBox.Core.Model.Repository;
using System.IO;

namespace WaveBox.ApiHandler.Handlers
{
	public class FanArtThumbnailApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "fanartthumb"; } }

		private readonly string cachePath = ServerUtility.RootPath() + "artistThumbnails" + Path.DirectorySeparatorChar;

		/// <summary>
		/// Process returns a file stream containing album art
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Check for the itemId
			if (uri.Id == null)
			{
				processor.WriteErrorHeader();
				return;
			}

			// Find the music brainz id
			string musicBrainzId = null;
			ItemType type = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId ((int)uri.Id);
			if (type == ItemType.Artist)
			{
				Artist artist = Injection.Kernel.Get<IArtistRepository>().ArtistForId(uri.Id);
				if (artist != null)
					musicBrainzId = artist.MusicBrainzId;
			}
			else if (type == ItemType.Artist)
			{
				AlbumArtist albumArtist = Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForId(uri.Id);
				if (albumArtist != null)
					musicBrainzId = albumArtist.MusicBrainzId;
			}

			if (musicBrainzId != null)
			{
				string path = ArtPathForMusicBrainzId(musicBrainzId);
				FileInfo info = new FileInfo(path);
				if (info.Exists)
				{
					DateTime? lastModified = info.LastWriteTimeUtc;
					FileStream stream = new FileStream(path, FileMode.Open);
					processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), null, true, lastModified);

					// Close the file so we don't get sharing violations on future accesses
					stream.Close();
				}
			}

			// If all else fails, error
			processor.WriteErrorHeader();
		}

		private string ArtPathForMusicBrainzId(string musicBrainzId)
		{
			if (musicBrainzId == null)
				return null;

			return cachePath + Path.DirectorySeparatorChar + musicBrainzId + ".jpg";
		}
	}
}

