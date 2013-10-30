using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Derived;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.OperationQueue;

namespace WaveBox.FolderScanning
{
	public class MusicBrainzScanOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "MusicBrainzScanOperation"; } }

		public MusicBrainzScanOperation(int delayMilliSeconds) : base(delayMilliSeconds)
		{
		}

		public override void Start()
		{
			// Stopwatches to track scanning times
			Stopwatch testTotalScanTime = new Stopwatch();
			Stopwatch testArtistScanTime = new Stopwatch();
			Stopwatch testAlbumArtistScanTime = new Stopwatch();

			// Dictionary of artists and existing IDs
			IDictionary<string, string> existingIds = new Dictionary<string, string>();

			// List of artists who don't have IDs
			IList<Artist> artistsMissingId = new List<Artist>();

			logger.IfInfo("------------- MUSICBRAINZ SCAN -------------");

			testTotalScanTime.Start();

			// Find artists and album artists missing ids, and all existing musicbrainz ids, to avoid extra lookups
			IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
			IList<Artist> allArtists = artistRepository.AllArtists();
			foreach (Artist artist in allArtists)
			{
				if (artist.MusicBrainzId == null)
				{
					artistsMissingId.Add(artist);
				}
				else
				{
					existingIds[artist.ArtistName] = artist.MusicBrainzId;
				}
			}

			IList<AlbumArtist> albumArtistsMissingId = new List<AlbumArtist>();

			IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
			IList<AlbumArtist> allAlbumArtists = albumArtistRepository.AllAlbumArtists();

			foreach (AlbumArtist albumArtist in allAlbumArtists)
			{
				if (albumArtist.MusicBrainzId == null)
				{
					albumArtistsMissingId.Add(albumArtist);
				}
				else
				{
					existingIds[albumArtist.AlbumArtistName] = albumArtist.MusicBrainzId;
				}
			}

			testArtistScanTime.Start();
			int artistCount = this.ScanArtists(existingIds, artistsMissingId);
			testArtistScanTime.Stop();

			testAlbumArtistScanTime.Start();
			int albumArtistCount = this.ScanAlbumArtists(existingIds, albumArtistsMissingId);
			testAlbumArtistScanTime.Stop();

			testTotalScanTime.Stop();

			logger.IfInfo("------------- MUSICBRAINZ SCAN -------------");
			logger.IfInfo("total scan time: " + testTotalScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("---------------------------------------------");
			logger.IfInfo("artist IDs retrieved: " + artistCount);
			logger.IfInfo("artist scan time: " + testArtistScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("albumArtist IDs retrieved: " + albumArtistCount);
			logger.IfInfo("albumArtist scan time: " + testAlbumArtistScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("---------------------------------------------");
		}

		private string MusicBrainzIdForArtistName(string artistName)
		{
			if (artistName == null)
			{
				return null;
			}

			try
			{
				// Set query address
				string address = "http://musicbrainz.herpderp.me:5000/ws/2/artist?query=\"" + System.Web.HttpUtility.UrlEncode(artistName) + "\"";

				// Capture XML response
				string responseXML = Injection.Kernel.Get<IWebClient>().DownloadString(address);

				try
				{
					XDocument doc = XDocument.Parse(responseXML);
					XElement firstElement = doc.Descendants().FirstOrDefault();
					if (firstElement != null)
					{
						XElement artistList = firstElement.Descendants().FirstOrDefault();
						if (artistList != null)
						{
							foreach (XElement artist in artistList.Descendants())
							{
								// Return the first id
								XAttribute idAttribute = artist.Attribute("id");
								if (idAttribute != null)
								{
									return idAttribute.Value;
								}
							}
						}
					}
				}
				catch (XmlException e)
				{
					logger.Error("Received malformed XML from server for " + artistName);
				}
				catch (Exception e)
				{
					logger.Error("Exception parsing musicbrainz response for " + artistName + ", " + e);
				}
			}
			// On timeout, report an error, but continue looping
			catch (WebException)
			{
				logger.Error("Request timed out for " + artistName);
			}
			catch (Exception e)
			{
				// Catch inner thread abort exception, which is caused when WaveBox is stopped during scan
				if (e.InnerException is ThreadAbortException)
				{
					logger.IfInfo("WaveBox is shutting down, canceling MusicBrainz Scan operation...");
				}
				else
				{
					// All other exceptions
					logger.Error("Exception contacting musicbrainz server for " + artistName + ", " + e);
				}
			}

			return null;
		}

		private int ScanArtists(IDictionary<string, string> existingIds, IList<Artist> artistsMissingId)
		{
			if (isRestart)
			{
				return 0;
			}

			// Lock to prevent race conditions on MusicBrainzID insert
			object artistsLock = new object();

			// Count of number of IDs retrieved
			int count = 0;

			IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
			Parallel.ForEach(artistsMissingId, artist =>
			{
				// First check if the id already exists
				string musicBrainzId = null;
				existingIds.TryGetValue(artist.ArtistName, out musicBrainzId);

				// If ID not found, try to fetch it
				if (musicBrainzId == null)
				{
					musicBrainzId = this.MusicBrainzIdForArtistName(artist.ArtistName);
				}

				if (musicBrainzId != null)
				{
					// We found one, so update the record and our cache
					lock (artistsLock)
					{
						existingIds[artist.ArtistName] = musicBrainzId;
						artist.MusicBrainzId = musicBrainzId;
						artistRepository.InsertArtist(artist, true);
						logger.IfInfo(artist.ArtistName + " = " + musicBrainzId);
						count++;
					}
				}
				else
				{
					logger.IfInfo("No musicbrainz id found for " + artist.ArtistName);
				}
			});

			return count;
		}

		private int ScanAlbumArtists(IDictionary<string, string> existingIds, IList<AlbumArtist> albumArtistsMissingId)
		{
			if (isRestart)
			{
				return 0;
			}

			// Lock to prevent race conditions on MusicBrainzID insert
			object albumArtistsLock = new object();

			// Count of number of IDs retrieved
			int count = 0;

			IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
			Parallel.ForEach(albumArtistsMissingId, albumArtist =>
			{
				// First check if the id already exists
				string musicBrainzId = null;
				existingIds.TryGetValue(albumArtist.AlbumArtistName, out musicBrainzId);

				// If ID not found, try to fetch it
				if (musicBrainzId == null)
				{
					musicBrainzId = this.MusicBrainzIdForArtistName(albumArtist.AlbumArtistName);
				}

				if (musicBrainzId != null)
				{
					// We found one, so update the record and our cache
					lock (albumArtistsLock)
					{
						existingIds[albumArtist.AlbumArtistName] = musicBrainzId;
						albumArtist.MusicBrainzId = musicBrainzId;
						albumArtistRepository.InsertAlbumArtist(albumArtist, true);
						logger.IfInfo(albumArtist.AlbumArtistName + " = " + musicBrainzId);
						count++;
					}
				}
				else
				{
					logger.IfInfo("No musicbrainz id found for " + albumArtist.AlbumArtistName);
				}
			});

			return count;
		}
	}
}
