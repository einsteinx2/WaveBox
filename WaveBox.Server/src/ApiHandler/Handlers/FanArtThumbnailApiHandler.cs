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

		// API handler is read-only, so no permissions checks needed
		public bool CheckPermission(User user, string action)
		{
			return true;
		}

		private readonly string cachePath = ServerUtility.RootPath() + "artistThumbnails" + Path.DirectorySeparatorChar;

		/// <summary>
		/// Process returns a file stream containing album art
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Check for the MusicBrainz ID
			string musicBrainzId = null;
			uri.Parameters.TryGetValue("musicBrainzId", out musicBrainzId);
			if (musicBrainzId == null)
			{
				processor.WriteErrorHeader();
				return;
			}

			// Find the music brainz id
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
			else
			{
				// If all else fails, error
				processor.WriteErrorHeader();
			}
		}

		private string ArtPathForMusicBrainzId(string musicBrainzId)
		{
			if (musicBrainzId == null)
			{
				return null;
			}

			return cachePath + Path.DirectorySeparatorChar + musicBrainzId + ".jpg";
		}
	}
}

