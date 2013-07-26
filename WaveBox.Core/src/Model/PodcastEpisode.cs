using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Model.Repository;

namespace WaveBox.Model
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM PodcastEpisode WHERE EpisodeId = ?", EpisodeId);

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
			EpisodeId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.PodcastEpisode);
			if (ReferenceEquals(EpisodeId, null))
			{
				return;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
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

				return podcastEpisode;
			}

			public PodcastEpisode CreatePodcastEpisode(long podcastId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
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
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				return new PodcastEpisode();
			}
		}
	}
}
