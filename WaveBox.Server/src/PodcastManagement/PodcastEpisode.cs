using System;
using System.Data;
using System.IO;
using WaveBox.Static;
using System.Net;
using System.Web;
using System.Xml;
using WaveBox.Model;
using System.Collections.Generic;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.PodcastManagement
{
	public class PodcastEpisode
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public long? EpisodeId { get; set; }
		public long? PodcastId { get; set; }
		public string Title { get; set; }
		public string Author { get; set; }
		public string Subtitle { get; set; }
		public string MediaUrl { get; set; }
		public string FilePath { get; set; }

		/* Constructors */
		public PodcastEpisode()
		{
		}

		/* Public methods */

		public bool Delete()
		{
			bool success = false;

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM PodcastEpisode WHERE EpisodeId = ?", EpisodeId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			if (File.Exists(FilePath))
			{
				File.Delete(FilePath);
			}

			return success;
		}

		public bool IsDownloaded()
		{
			return true;
		}

		public void AddToDatabase()
		{
			EpisodeId = Item.GenerateItemId(ItemType.PodcastEpisode);
			if (ReferenceEquals(EpisodeId, null))
				return;

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.InsertLogged(this);

				if (affected == 0)
				{
					EpisodeId = null;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}
		}

		public class Factory
		{
			public PodcastEpisode CreatePodcastEpisode(XmlNode episode, XmlNamespaceManager mgr, long? podcastId)
			{
				if (podcastId == null)
				{
					return new PodcastEpisode();
				}

				var podcastEpisode = new PodcastEpisode();
				podcastEpisode.PodcastId = podcastId;
				podcastEpisode.Title = episode.SelectSingleNode("title").InnerText;
				podcastEpisode.Author = episode.SelectSingleNode("itunes:author", mgr).InnerText;
				podcastEpisode.Subtitle = episode.SelectSingleNode("itunes:subtitle", mgr).InnerText;
				podcastEpisode.MediaUrl = episode.SelectSingleNode("enclosure").Attributes["url"].InnerText;
				//if (logger.IsInfoEnabled) logger.Info(episode.SelectSingleNode("title").InnerText);
				//if (logger.IsInfoEnabled) logger.Info(episode.SelectSingleNode("itunes:author", mgr).InnerText);
				//if (logger.IsInfoEnabled) logger.Info(episode.SelectSingleNode("itunes:subtitle", mgr).InnerText);
				//if (logger.IsInfoEnabled) logger.Info(episode.SelectSingleNode("enclosure").Attributes["url"].InnerText);
				//if (logger.IsInfoEnabled) logger.Info();

				return podcastEpisode;
			}

			public PodcastEpisode CreatePodcastEpisode(long podcastId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<PodcastEpisode>("SELECT * FROM PodcastEpisode WHERE EpisodeId = ? LIMIT 1", podcastId);

					foreach (var episode in result)
					{
						return episode;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return new PodcastEpisode();
			}
		}
	}
}

