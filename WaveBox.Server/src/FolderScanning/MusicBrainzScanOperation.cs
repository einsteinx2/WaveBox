using System;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using System.Collections.Generic;
using WaveBox.Core.OperationQueue;
using WaveBox.Core;
using WaveBox.Core.Model.Repository;
using System.Diagnostics;
using System.Linq;
using Ninject;
using System.Net;
using System.Xml.Linq;
using System.Text;

namespace WaveBox.FolderScanning
{
	public class MusicBrainzScanOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "MusicBrainzScanOperation"; } }

		private IList<MusicBrainzCheckDate> checkDates;

		private IDictionary<string, string> existingIds = new Dictionary<string, string>();
		private IList<Artist> artistsMissingId = new List<Artist>();
		private IList<AlbumArtist> albumArtistsMissingId = new List<AlbumArtist>();

		Stopwatch testTotalScanTime = new Stopwatch();
		Stopwatch testArtistScanTime = new Stopwatch();
		Stopwatch testAlbumArtistScanTime = new Stopwatch();

		public MusicBrainzScanOperation(int delayMilliSeconds) : base(delayMilliSeconds)
		{
		}

		public override void Start()
		{
			logger.IfInfo("------------- MUSICBRAINZ SCAN -------------");

			// Find all of the previous attempts so we don't retry too quickly
			long timestamp = DateTime.UtcNow.AddDays(-1).ToUniversalUnixTimestamp();
			checkDates = Injection.Kernel.Get<IMusicBrainzCheckDateRepository>().AllCheckDatesOlderThan(timestamp);

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
			ScanArtists();
			testArtistScanTime.Stop();

			testAlbumArtistScanTime.Start();
			ScanAlbumArtists();
			testAlbumArtistScanTime.Stop();

			testTotalScanTime.Stop();

			logger.IfInfo("------------- MUSICBRAINZ SCAN -------------");
			logger.IfInfo("total scan time: " + testTotalScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("---------------------------------------------");
			logger.IfInfo("artist scan time: " + testArtistScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("albumArtist scan time: " + testAlbumArtistScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("---------------------------------------------");
		}

		private string MusicBrainzIdForArtistName(string artistName)
		{
			if (artistName == null)
				return null;

			try
			{
				using (WebClient client = new WebClient())
				{
					string address = "http://herpderp.me:5000/ws/2/artist?query=" + System.Web.HttpUtility.UrlEncode(artistName);
					//string address = "http://musicbrainz.org/ws/2/artist?query=" + System.Web.HttpUtility.UrlEncode(artistName);
					string responseXML = client.DownloadString(address);

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
					catch (Exception e)
					{
						logger.Error("Exception parsing musicbrainz response for " + artistName + ", " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Exception contacting musicbrainz server for " + artistName + ", " + e);
			}

			return null;
		}

		private void ScanArtists()
		{
			IMusicBrainzCheckDateRepository musicBrainzCheckDateRepository = Injection.Kernel.Get<IMusicBrainzCheckDateRepository>();
			IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
			foreach (Artist artist in artistsMissingId)
			{
				if (isRestart)
					return;

				if (artist.ArtistName == null)
					continue;

				// First check if the id already exists
				string musicBrainzId = null;
				existingIds.TryGetValue(artist.ArtistName, out musicBrainzId);
				if (musicBrainzId == null)
				{
					// Only process if we didn't recently try
					if (checkDates.SingleOrDefault(d => d.ItemId == artist.ArtistId) == null)
					{
						string id = MusicBrainzIdForArtistName(artist.ArtistName);
						if (id == null)
						{
							// Store the time we tried, so we don't try again for a while
							MusicBrainzCheckDate checkDate = new MusicBrainzCheckDate((int)artist.ArtistId);
							musicBrainzCheckDateRepository.InsertMusicBrainzCheckDate(checkDate);
						}
						else
						{
							// We found an ID
							musicBrainzId = id;
						}
					}
				}

				if (musicBrainzId != null)
				{
					// We found one, so update the record and our cache
					existingIds[artist.ArtistName] = musicBrainzId;
					artist.MusicBrainzId = musicBrainzId;
					artistRepository.InsertArtist(artist, true);
					logger.IfInfo(artist.ArtistName + " = " + musicBrainzId);
				}
			}
		}

		private void ScanAlbumArtists()
		{
			IMusicBrainzCheckDateRepository musicBrainzCheckDateRepository = Injection.Kernel.Get<IMusicBrainzCheckDateRepository>();
			IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
			foreach (AlbumArtist albumArtist in albumArtistsMissingId)
			{
				if (isRestart)
				{
					return;
				}

				// First check if the id already exists
				string musicBrainzId = null;
				existingIds.TryGetValue(albumArtist.AlbumArtistName, out musicBrainzId);
				if (musicBrainzId == null)
				{
					// Only process if we didn't recently try
					if (checkDates.SingleOrDefault(d => d.ItemId == albumArtist.AlbumArtistId) == null)
					{
						string id = MusicBrainzIdForArtistName(albumArtist.AlbumArtistName);
						if (id == null)
						{
							// Store the time we tried, so we don't try again for a while
							MusicBrainzCheckDate checkDate = new MusicBrainzCheckDate((int)albumArtist.AlbumArtistId);
							musicBrainzCheckDateRepository.InsertMusicBrainzCheckDate(checkDate);
						}
						else
						{
							// We found an ID
							musicBrainzId = id;
						}
					}
				}

				if (musicBrainzId != null)
				{
					// We found one, so update the record and our cache
					existingIds[albumArtist.AlbumArtistName] = musicBrainzId;
					albumArtist.MusicBrainzId = musicBrainzId;
					albumArtistRepository.InsertAlbumArtist(albumArtist, true);
					logger.IfInfo(albumArtist.AlbumArtistName + " = " + musicBrainzId);
				}
			}
		}
	}
}

