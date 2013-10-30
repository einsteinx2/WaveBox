using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Derived;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;

namespace WaveBox.FolderScanning
{
	public class ArtistThumbnailDownloadOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "ArtistThumbnailDownloadOperation"; } }

		private readonly string cachePath = ServerUtility.RootPath() + "artistThumbnails" + Path.DirectorySeparatorChar;

		public ArtistThumbnailDownloadOperation(int delayMilliSeconds) : base(delayMilliSeconds)
		{
		}

		public override void Start()
		{
			logger.IfInfo("------------- ARTIST ART SCAN -------------");

			Stopwatch testTotalScanTime = new Stopwatch();
			testTotalScanTime.Start();

			// Create the cache directory if it doesn't exist yet
			if (!Directory.Exists(cachePath))
			{
				Directory.CreateDirectory(cachePath);
			}

			// Keep a set of all MusicBrainz IDs known to WaveBox
			ISet<string> musicBrainzIds = new HashSet<string>();

			// Find artists and album artists missing art
			IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
			IList<Artist> allArtists = artistRepository.AllArtists();
			foreach (Artist artist in allArtists)
			{
				string musicBrainzId = artist.MusicBrainzId;
				if (musicBrainzId != null)
				{
					if (!File.Exists(this.ArtPathForMusicBrainzId(musicBrainzId)))
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
					if (!File.Exists(this.ArtPathForMusicBrainzId(musicBrainzId)))
					{
						musicBrainzIds.Add(musicBrainzId);
					}
				}
			}

			// Scan all MusicBrainz IDs collected by WaveBox
			int downloadCount = this.ScanIds(musicBrainzIds);

			testTotalScanTime.Stop();

			logger.IfInfo("------------- ARTIST ART SCAN -------------");
			logger.IfInfo("items retrieved: " + downloadCount);
			logger.IfInfo("total scan time: " + testTotalScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("-------------------------------------------");
		}

		// Return the local art path for a given MusicBrainz ID
		private string ArtPathForMusicBrainzId(string musicBrainzId)
		{
			if (musicBrainzId == null)
			{
				return null;
			}

			return cachePath + Path.DirectorySeparatorChar + musicBrainzId + ".jpg";
		}

		// Scan all MusicBrainz IDs in set, and attempt to download and cache art
		private int ScanIds(ISet<string> musicBrainzIds)
		{
			// Track the number of items successfully downloaded
			int downloadCount = 0;

			Parallel.ForEach(musicBrainzIds, musicBrainzId =>
			{
				try
				{
					// Set query address and art download path
					string address = "http://fanart1.waveboxapp.com:8000?action=art&type=artist&preview=1&id=" + musicBrainzId;
					string path = this.ArtPathForMusicBrainzId(musicBrainzId);

					// Set web client default timeout in milliseconds
					int timeout = 5000;

					// SUPER HACK: Linux WebRequest libraries are really bad, so call curl to speed things up
					if (WaveBoxService.Platform == "Linux")
					{
						new LinuxWebClient(timeout).DownloadFile(address, path);
					}
					else
					{
						// All other operating systems, use TimedWebClient
						new TimedWebClient(timeout).DownloadFile(address, path);
					}

					// Make sure the file has contents, otherwise delete it
					FileInfo info = new FileInfo(path);
					if (info.Exists && info.Length == 0)
					{
						File.Delete(path);
					}
					else
					{
						logger.IfInfo("Downloaded art for " + musicBrainzId);
						downloadCount++;
					}
				}
				// On timeout, report an error, but continue looping
				catch (WebException)
				{
					logger.Error("Request timed out for " + musicBrainzId);
				}
				catch (Exception e)
				{
					logger.Error("Exception contacting fanart server for " + musicBrainzId + ", " + e);
				}
			});

			// Return number of successful downloads
			return downloadCount;
		}
	}
}
