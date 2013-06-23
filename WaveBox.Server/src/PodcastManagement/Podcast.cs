using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using WaveBox.Static;
using WaveBox.Model;
using System.Collections.Concurrent;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Injected;
using Ninject;

namespace WaveBox.PodcastManagement
{
	public class Podcast
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly string PodcastMediaDirectory = Injection.Kernel.Get<IServerSettings>().PodcastFolder;

		/* IVars */
		protected string rssUrl;
		protected XmlDocument doc;
		protected XmlNamespaceManager mgr;

		/* Properties */
		public long? PodcastId { get; set; }
		public long? EpisodeKeepCap { get; set; } 
		public string Title { get; set; }
		public string Author { get; set; }
		public string Description { get; set; }

		/* Constructors */
		public Podcast()
		{
		}
		
		/* Instance methods */
		public void AddToDatabase()
		{
			PodcastId = Item.GenerateItemId(ItemType.Podcast);
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.InsertLogged(this);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public void DownloadNewEpisodes()
		{
			if (!Directory.Exists(PodcastMediaDirectory))
			{
				Directory.CreateDirectory(PodcastMediaDirectory);
			}
			if (!Directory.Exists(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title))
			{
				Directory.CreateDirectory(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title);
			}

			List<PodcastEpisode> current = ListOfCurrentEpisodes();
			List<PodcastEpisode> stored = ListOfStoredEpisodes();
			List<PodcastEpisode> newEps = new List<PodcastEpisode>();

			// get new episodes
			foreach (PodcastEpisode currentEp in current)
			{
				bool epIsNew = true;
				foreach (PodcastEpisode storedEp in stored)
				{
					if (storedEp.Title == currentEp.Title)
					{
						epIsNew = false;
					}
				}

				if (epIsNew)
				{
					newEps.Add(currentEp);
				}
			}

			if (stored.Count == EpisodeKeepCap && newEps.Count > 0)
			{
				DeleteOldEpisodes(newEps.Count);
			} 

			DownloadQueue.Enqueue(newEps);
		}

		private void DeleteOldEpisodes(int count)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<PodcastEpisode>("SELECT * FROM PodcastEpisode WHERE PodcastId = ? ORDER BY EpisodeId LIMIT ?", PodcastId, count);

				foreach (var episode in result)
				{
					episode.Delete();
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public bool Delete()
		{
			// remove any episodes of this podcast that might happen to be in the download queue
			if (this.PodcastId != null)
			{
				DownloadQueue.RemovePodcast(PodcastId.Value);
			}

			foreach (PodcastEpisode ep in ListOfStoredEpisodes())
			{
				ep.Delete();
			}

			bool success = false;

			// remove podcast entry
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Podcast WHERE PodcastId = ?", PodcastId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			if (Directory.Exists(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title))
			{
				Directory.Delete(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title);
			}

			return success;
		}

		public List<PodcastEpisode> ListOfCurrentEpisodes()
		{
			var doc = new XmlDocument();
			doc.Load(rssUrl);
			mgr = new XmlNamespaceManager(doc.NameTable);
			mgr.AddNamespace("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd");
			var xmlList = doc.SelectNodes("//item");
			List<PodcastEpisode> list = new List<PodcastEpisode>();

			// Make sure we don't try to add more episodes than there actually are.
			long? j = EpisodeKeepCap <= xmlList.Count ? EpisodeKeepCap : list.Count;
			for (int i = 0; i < j; i++)
			{
				list.Add(new PodcastEpisode.Factory().CreatePodcastEpisode(xmlList.Item(i), mgr, PodcastId));
			}
			return list;
		}

		public List<PodcastEpisode> ListOfStoredEpisodes()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<PodcastEpisode>("SELECT * FROM PodcastEpisode WHERE PodcastId = ?", PodcastId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<PodcastEpisode>();
		}

		/* Class methods */
		public static List<Podcast> ListOfStoredPodcasts()
		{	
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Podcast>("SELECT * FROM Podcast ORDER BY Title DESC");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
			return new List<Podcast>();
		}

		private static int? PodcastIdForRssUrl(string rss)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int podcastId = conn.ExecuteScalar<int>("SELECT PodcastId FROM Podcast WHERE RssUrl = ?", rss);

				return podcastId == 0 ? (int?)null : podcastId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return null;
		}

		public class Factory
		{
			public Podcast CreatePodcast(string rss, int keepCap)
			{
				var podcast = new Podcast();

				podcast.rssUrl = rss;
				podcast.EpisodeKeepCap = keepCap;

				XmlNode root, channel;
				podcast.doc = new XmlDocument();
				podcast.doc.Load(podcast.rssUrl);
				podcast.mgr = new XmlNamespaceManager(podcast.doc.NameTable);
				podcast.mgr.AddNamespace("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd");

				root = podcast.doc.DocumentElement;
				channel = root.SelectSingleNode("descendant::channel");

				podcast.Title = channel.SelectSingleNode("title").InnerText;
				podcast.Author = channel.SelectSingleNode("itunes:author", podcast.mgr).InnerText;
				podcast.Description = channel.SelectSingleNode("description").InnerText;

				int? existingPodcastId = PodcastIdForRssUrl(rss);
				if (existingPodcastId != null)
				{
					Podcast existing = new Podcast.Factory().CreatePodcast(existingPodcastId);
					podcast.PodcastId = existingPodcastId;
					podcast.EpisodeKeepCap = existing.EpisodeKeepCap;
					podcast.Title = existing.Title;
					podcast.Author = existing.Author;
					podcast.Description = existing.Description;
				}
				else
				{
					podcast.AddToDatabase();
				}

				return podcast;
			}

			public Podcast CreatePodcast(long? podcastId)
			{
				if (podcastId == null)
				{
					return new Podcast();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.Query<Podcast>("SELECT * FROM Podcast WHERE PodcastId = ? LIMIT 1", podcastId);

					foreach (var p in result)
					{
						return p;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				var podcast = new Podcast();
				podcast.PodcastId = podcastId.Value;
				return podcast;
			}
		}
	}
}

