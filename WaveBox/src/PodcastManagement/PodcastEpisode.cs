using System;
using System.Data;
using System.IO;
using WaveBox.DataModel.Singletons;
using System.Net;
using System.Web;
using System.Xml;
using WaveBox.DataModel.Model;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace WaveBox.PodcastManagement
{
	public class PodcastEpisode
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public long? EpisodeId { get; set; }
		public long? PodcastId { get; set; }
		public string Title { get; set; }
		public string Author { get; set; }
		public string Subtitle { get; set; }
		public string MediaUrl { get; set; }
		public string FilePath { get; set; }

		/* Constructors */
		public PodcastEpisode(XmlNode episode, XmlNamespaceManager mgr, long? podcastId)
		{
			if (podcastId == null)
			{
				return;
			}

			PodcastId = podcastId;
			Title = episode.SelectSingleNode("title").InnerText;
			Author = episode.SelectSingleNode("itunes:author", mgr).InnerText;
			Subtitle = episode.SelectSingleNode("itunes:subtitle", mgr).InnerText;
			MediaUrl = episode.SelectSingleNode("enclosure").Attributes["url"].InnerText;
			//logger.Info(episode.SelectSingleNode("title").InnerText);
			//logger.Info(episode.SelectSingleNode("itunes:author", mgr).InnerText);
			//logger.Info(episode.SelectSingleNode("itunes:subtitle", mgr).InnerText);
			//logger.Info(episode.SelectSingleNode("enclosure").Attributes["url"].InnerText);
			//logger.Info();
		}

		public PodcastEpisode(long podcastId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("SELECT * FROM podcast_episode WHERE podcast_episode_id = @podcastid", conn);
				q.AddNamedParam("@podcastid", podcastId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
				else
				{
					logger.Info("Podcast constructor query returned no results");
				}
			}
			catch (Exception e)
			{
				logger.Info("[PODCASTEPISODE(1)] ERROR: " +  e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		/* Public methods */

		public bool Delete()
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			bool success = false;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("DELETE FROM podcast_episode WHERE podcast_episode_id = @podcastid", conn);
				q.AddNamedParam("@podcastid", EpisodeId);
				q.Prepare();
				success = q.ExecuteNonQueryLogged() >= 1;
			}
			catch (Exception e)
			{
				logger.Info("[PODCASTEPISODE(1)] ERROR: " +  e);
			}
			finally
			{
				Database.Close(conn, reader);
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
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO podcast_episode (podcast_episode_id, podcast_episode_podcast_id, podcast_episode_title, podcast_episode_author, podcast_episode_subtitle, podcast_episode_media_url, podcast_episode_file_path) " + 
													 "VALUES (@pe_id, @pe_pid, @pe_title, @pe_author, @pe_subtitle, @pe_mediaurl, @pe_filepath)", conn);
				q.AddNamedParam("@pe_id", EpisodeId);
				q.AddNamedParam("@pe_pid", PodcastId);
				q.AddNamedParam("@pe_title", Title);
				q.AddNamedParam("@pe_author", Author);
				q.AddNamedParam("@pe_subtitle", Subtitle);
				q.AddNamedParam("@pe_mediaurl", MediaUrl);
				q.AddNamedParam("@pe_filepath", FilePath);
				q.Prepare();

				q.ExecuteNonQueryLogged();
			}
			catch (Exception e)
			{
				logger.Info("[PODCAST (1)] ERROR: " +  e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		/* Private methods */

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			EpisodeId = reader.GetInt32(reader.GetOrdinal("podcast_episode_id"));
			PodcastId = reader.GetInt32(reader.GetOrdinal("podcast_episode_podcast_id"));
			Title = reader.GetString(reader.GetOrdinal("podcast_episode_title"));
			Author = reader.GetString(reader.GetOrdinal("podcast_episode_author"));
			Subtitle = reader.GetString(reader.GetOrdinal("podcast_episode_subtitle"));
			MediaUrl = reader.GetString(reader.GetOrdinal("podcast_episode_media_url"));
			FilePath = reader.GetString(reader.GetOrdinal("podcast_episode_file_path"));
		}
	}
}

