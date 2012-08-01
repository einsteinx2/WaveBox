using System;
using System.Data;
using System.IO;
using WaveBox.DataModel.Singletons;
using System.Net;
using System.Web;
using System.Xml;
using System.Collections.Generic;
using System.Threading;


namespace WaveBox.Podcast
{
    public class PodcastEpisode
    {
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
            if(podcastId == null) return;

            PodcastId = podcastId;
            Title = episode.SelectSingleNode("title").InnerText;
            Author = episode.SelectSingleNode("itunes:author", mgr).InnerText;
            Subtitle = episode.SelectSingleNode("itunes:subtitle", mgr).InnerText;
            MediaUrl = episode.SelectSingleNode("enclosure").Attributes["url"].InnerText;
            Console.WriteLine(episode.SelectSingleNode("title").InnerText);
            Console.WriteLine(episode.SelectSingleNode("itunes:author", mgr).InnerText);
            Console.WriteLine(episode.SelectSingleNode("itunes:subtitle", mgr).InnerText);
            Console.WriteLine(episode.SelectSingleNode("enclosure").Attributes["url"].InnerText);
            Console.WriteLine();
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
                    SetPropertiesFromQueryResult(reader);
                }
                else
                {
                    Console.WriteLine("Podcast constructor query returned no results");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[ARTIST(1)] ERROR: " +  e.ToString());
            }
            finally
            {
                Database.Close(conn, reader);
            }
        }

        /* Public methods */

        public void Delete()
        {
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("DELETE FROM podcast_episode WHERE podcast_episode_id = @podcastid", conn);
                q.AddNamedParam("@podcastid", EpisodeId);
                q.Prepare();
                q.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("[ARTIST(1)] ERROR: " +  e.ToString());
            }
            finally
            {
                Database.Close(conn, reader);
            }

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        public bool IsDownloaded()
        {
            return true;
        }



        public void AddToDatabase()
        {
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO podcast_episode (podcast_episode_podcast_id, podcast_episode_title, podcast_episode_author, podcast_episode_subtitle, podcast_episode_media_url, podcast_episode_file_path) " + 
                                                     "VALUES (@pe_pid, @pe_title, @pe_author, @pe_subtitle, @pe_mediaurl, @pe_filepath)", conn);
                q.AddNamedParam("@pe_pid", PodcastId);
                q.AddNamedParam("@pe_title", Title);
                q.AddNamedParam("@pe_author", Author);
                q.AddNamedParam("@pe_subtitle", Subtitle);
                q.AddNamedParam("@pe_mediaurl", MediaUrl);
                q.AddNamedParam("@pe_filepath", FilePath);
                q.Prepare();

                int affected = q.ExecuteNonQuery();

                if (affected > 0)
                {
                    q.CommandText = "SELECT last_insert_rowid()";
                    PodcastId = Convert.ToInt32(q.ExecuteScalar().ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (1)] ERROR: " +  e.ToString());
            }
            finally
            {
                Database.Close(conn, reader);
            }
        }

        /* Private methods */

        private void SetPropertiesFromQueryResult(IDataReader reader)
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

