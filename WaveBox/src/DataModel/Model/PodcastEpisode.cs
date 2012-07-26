using System;
using System.Data;
using System.IO;
using WaveBox.DataModel.Singletons;
using System.Net;
using System.Web;
using System.Xml;


namespace PodcastParsing
{
    public class PodcastEpisode
    {
        public int? EpisodeId { get; set; }
        public int? PodcastId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Subtitle { get; set; }
        public string MediaUrl { get; set; }
        public string FilePath { get; set; }

        public PodcastEpisode(XmlNode episode, XmlNamespaceManager mgr, int? podcastId)
        {
            Console.WriteLine(episode.SelectSingleNode("title").InnerText);
            Console.WriteLine(episode.SelectSingleNode("itunes:author", mgr).InnerText);
            Console.WriteLine(episode.SelectSingleNode("itunes:subtitle", mgr).InnerText);
            Console.WriteLine(episode.SelectSingleNode("enclosure").Attributes["url"].InnerText);
            Console.WriteLine();
        }

        public PodcastEpisode(int podcastId)
        {
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("SELECT * FROM podcast WHERE podcast_id = @podcastid", conn);
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

        public PodcastEpisode()
        {
        }

        public void Delete()
        {
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("DELETE FROM podcast WHERE podcast_id = @podcastid", conn);
                q.AddNamedParam("@podcastid", PodcastId);
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

        public void Download()
        {
            IAsyncResult r = null;
            var req = (HttpWebRequest)WebRequest.Create(MediaUrl);

            req.BeginGetResponse(result => 
            {
                WebResponse f = req.EndGetResponse(r);
                f.
            }, 
            null);



        }

        public int DownloadProgress()
        {
            return 1;
        }

        private void SetPropertiesFromQueryResult(IDataReader reader)
        {
            EpisodeId = reader.GetInt32(reader.GetOrdinal("podcast_episode_id"));
            PodcastId = reader.GetInt32(reader.GetOrdinal("podcast_episode_podcast_id"));
            Title = reader.GetString(reader.GetOrdinal("podcast_episode_title"));
            Author = reader.GetString(reader.GetOrdinal("podcast_episode_author"));
            Subtitle = reader.GetString(reader.GetOrdinal("podcast_episode_subtitle"));
            MediaUrl = reader.GetString(reader.GetOrdinal("podcast_episode_media_url"));
        }
    }
}

