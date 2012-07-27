using System;
using System.Data;
using System.IO;
using WaveBox.DataModel.Singletons;
using System.Net;
using System.Web;
using System.Xml;
using System.Collections.Generic;
using System.Threading;


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

        public static DownloadQueue Dq { get; set; }
        public static Object CurrentlyDownloadingLock = new Object();  
        public static WebClient webClient = new WebClient();
        private long contentLength, totalBytesRead;

        public PodcastEpisode(XmlNode episode, XmlNamespaceManager mgr, int? podcastId)
        {
            if(podcastId == null) return;
            if(Dq == null) Dq = new DownloadQueue();

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

        public PodcastEpisode(int podcastId)
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

        public void Delete()
        {
//            // if we remove this item from the queue, then it's not in the database yet, so we can just return.
//            if(Dq.RemoveItem(this, webClient)) return;

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

        public void StartDownload()
        {
            webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => 
            {
                if (contentLength == 0)
                    contentLength = e.TotalBytesToReceive;
                Console.WriteLine(this.Title + ": " + ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100 + "%");
                totalBytesRead = e.BytesReceived;
            });

            webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler((sender, e) => 
            {
                AddToDatabase();
                if(Dq.Count > 0)
                {
                    webClient.CancelAsync();
                    Dq.Dequeue();
                    if(Dq.CurrentItem() != null)
                        Dq.CurrentItem().StartDownload();
                }
            });

            var uri = new Uri(MediaUrl);
            string[] fns = MediaUrl.Split('/');
            string fn = fns[fns.Length - 1];
            var pc = new Podcast(PodcastId);
            FilePath = Podcast.PodcastMediaDirectory + Path.DirectorySeparatorChar + pc.Title + Path.DirectorySeparatorChar + fn;

            webClient.DownloadFileAsync(uri, FilePath);
        }

        public void QueueDownload()
        {
            Dq.Enqueue(this);
            if (!webClient.IsBusy)
            {
                Dq.CurrentItem().StartDownload();
            }

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

        public double DownloadProgress()
        {
            return (double)totalBytesRead / (double)contentLength;
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

