using System;
using System.Diagnostics;
using System.Collections.Generic;
using WaveBox.Core.Model;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model.Repository;
using WaveBox.Core;
using System.IO;
using WaveBox.Core.OperationQueue;
using Ninject;
using System.Net;

namespace WaveBox.FolderScanning
{
	public class ArtistThumbnailDownloadOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "ArtistThumbnailDownloadOperation"; } }

		private readonly string cachePath = ServerUtility.RootPath() + "artistThumbnails" + Path.DirectorySeparatorChar;

		private ISet<string> musicBrainzIds = new HashSet<string>();

		Stopwatch testTotalScanTime = new Stopwatch();

		public ArtistThumbnailDownloadOperation(int delayMilliSeconds) : base(delayMilliSeconds)
		{
		}

		public override void Start()
		{
			logger.IfInfo("------------- ARTIST ART SCAN -------------");
			
			testTotalScanTime.Start();

			// Create the cache directory if it doesn't exist yet
			if (!Directory.Exists(cachePath))
				Directory.CreateDirectory(cachePath);

			// Find artists and album artists missing art
			IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
			IList<Artist> allArtists = artistRepository.AllArtists();
			foreach (Artist artist in allArtists)
			{
				string musicBrainzId = artist.MusicBrainzId;
				if (musicBrainzId != null)
				{
					if (!File.Exists(ArtPathForMusicBrainzId(musicBrainzId)))
					{
						musicBrainzIds.Add(musicBrainzId);
					}
				}
			}

			IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
			IList<AlbumArtist> allAlbumArtists = albumArtistRepository.AllAlbumArtists();
			foreach (AlbumArtist albumArtist in allAlbumArtists)
			{
				string musicBrainzId = albumArtist.MusicBrainzId;
				if (musicBrainzId != null)
				{
					if (!File.Exists(ArtPathForMusicBrainzId(musicBrainzId)))
					{
						musicBrainzIds.Add(musicBrainzId);
					}
				}
			}

			ScanIds();

			testTotalScanTime.Stop();

			logger.IfInfo("------------- ARTIST ART SCAN -------------");
			logger.IfInfo("total scan time: " + testTotalScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("-------------------------------------------");
		}

		private string ArtPathForMusicBrainzId(string musicBrainzId)
		{
			if (musicBrainzId == null)
				return null;

			return cachePath + Path.DirectorySeparatorChar + musicBrainzId + ".jpg";
		}

		private string ScanIds()
		{
			foreach (string musicBrainzId in musicBrainzIds)
			{
				try
				{
					using (WebClient client = new WebClient())
					{
						string address = "http://herpderp.me:8000?action=art&type=artist&preview=1&id=" + musicBrainzId;
						string path = ArtPathForMusicBrainzId(musicBrainzId);
						client.DownloadFile(address, path);

						// Make sure the file has contents, otherwise delete it
						FileInfo info = new FileInfo(path);
						if (info.Exists && info.Length == 0)
						{
							File.Delete(path);
						}
						else
						{
							logger.IfInfo("Downloaded art for " + musicBrainzId);
						}
					}
				}
				catch (Exception e)
				{
					logger.Error("Exception contacting fanart server for " + musicBrainzId + ", " + e);
				}
			}

			return null;
		}
	}
}

