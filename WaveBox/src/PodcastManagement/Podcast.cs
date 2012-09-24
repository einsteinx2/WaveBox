using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Data;
using System.Collections.Concurrent;

namespace WaveBox.PodcastManagement
{
    public class Podcast
    {
        public static readonly string PodcastMediaDirectory = Settings.PodcastFolder;

        /* IVars */
        private string rssUrl;
        XmlDocument doc;
        XmlNamespaceManager mgr;

        /* Properties */
        public long? PodcastId { get; set; }
        public long? EpisodeKeepCap { get; set; } 
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }

        /* Constructors */
        public Podcast(string rss, int keepCap)
        {
            rssUrl = rss;
            EpisodeKeepCap = keepCap;

            XmlNode root, channel;
            doc = new XmlDocument();
            doc.Load(rssUrl);
            mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd");

            root = doc.DocumentElement;
            channel = root.SelectSingleNode("descendant::channel");

            Title = channel.SelectSingleNode("title").InnerText;
            Author = channel.SelectSingleNode("itunes:author", mgr).InnerText;
            Description = channel.SelectSingleNode("description").InnerText;

            int? existingPodcastId = PodcastIdForRssUrl(rss);
            if (existingPodcastId != null)
            {
                Podcast existing = new Podcast(existingPodcastId);
                PodcastId = existingPodcastId;
                EpisodeKeepCap = existing.EpisodeKeepCap;
                Title = existing.Title;
                Author = existing.Author;
                Description = existing.Description;
                return;
            }
            else AddToDatabase();

            //Console.WriteLine(Title + "\r\n " + Author + "\r\n " + Description + "\r\n\r\n");
        }

        public Podcast(long? podcastId)
        {
            if(podcastId == null) return;

            PodcastId = podcastId.Value;

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
                    EpisodeKeepCap = reader.GetInt32(reader.GetOrdinal("podcast_keep_cap"));
                    Title = reader.GetString(reader.GetOrdinal("podcast_title"));
                    Author = reader.GetString(reader.GetOrdinal("podcast_author"));
                    Description = reader.GetString(reader.GetOrdinal("podcast_description"));
                    rssUrl = reader.GetString(reader.GetOrdinal("podcast_rss_url"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (2)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
        }

        public Podcast(IDataReader reader)
        {
            SetPropertiesFromQueryReader(reader);
        }

        /* Instance methods */
        public void AddToDatabase()
        {
            PodcastId = Item.GenerateItemId(ItemType.Podcast);
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO podcast (podcast_id, podcast_keep_cap, podcast_title, podcast_author, podcast_description, podcast_rss_url) VALUES (@id, @keepcap, @title, @author, @desc, @rss)", conn);
                q.AddNamedParam("@id", PodcastId);
                q.AddNamedParam("@keepcap", EpisodeKeepCap.Value);
                q.AddNamedParam("@title", Title);
                q.AddNamedParam("@author", Author);
                q.AddNamedParam("@desc", Description);
                q.AddNamedParam("@rss", rssUrl);
                q.Prepare();

                q.ExecuteNonQueryLogged();
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (1)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
        }

        public void DownloadNewEpisodes()
        {
            if (!Directory.Exists(PodcastMediaDirectory)) Directory.CreateDirectory(PodcastMediaDirectory);
            if (!Directory.Exists(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title)) Directory.CreateDirectory(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title);

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
                        epIsNew = false;
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
            IDbConnection conn = null;
            IDataReader reader = null;

            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("SELECT podcast_episode_id FROM podcast_episode WHERE podcast_episode_podcast_id = @podcastid ORDER BY podcast_episode_id LIMIT @count", conn);
                q.AddNamedParam("@podcastid", PodcastId);
                q.AddNamedParam("@count", count);
                q.Prepare();
                reader = q.ExecuteReader();

                while (reader.Read())
                {
                    new PodcastEpisode(reader.GetInt32(0)).Delete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (2)] ERROR: " + e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
        }

        public bool Delete()
        {
            // remove any episodes of this podcast that might happen to be in the download queue
            if(this.PodcastId != null)
                DownloadQueue.RemovePodcast(PodcastId.Value);

            foreach (PodcastEpisode ep in ListOfStoredEpisodes())
            {
                ep.Delete();
            }

            IDbConnection conn = null;
            IDataReader reader = null;
            bool success = false;

            // remove podcast entry
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("DELETE FROM podcast WHERE podcast_id = @podcastid", conn);
                q.AddNamedParam("@podcastid", PodcastId);
                q.Prepare();
                success = q.ExecuteNonQueryLogged() >= 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (2)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }

            if(Directory.Exists(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title)) Directory.Delete(PodcastMediaDirectory + Path.DirectorySeparatorChar + Title);

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
            for(int i = 0; i < j; i++)
            {
                list.Add(new PodcastEpisode(xmlList.Item(i), mgr, PodcastId));
            }
            return list;
        }

        public List<PodcastEpisode> ListOfStoredEpisodes()
        {
            List<PodcastEpisode> list = new List<PodcastEpisode>();
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("SELECT podcast_episode_id FROM podcast_episode WHERE podcast_episode_podcast_id = @podcastid", conn);
                q.AddNamedParam("podcastid", PodcastId);
                q.Prepare();
                reader = q.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new PodcastEpisode(reader.GetInt32(0)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (3)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
            return list;
        }

        public void SetPropertiesFromQueryReader(IDataReader reader)
        {
            PodcastId = reader.GetInt64(reader.GetOrdinal("podcast_id"));
            EpisodeKeepCap = reader.GetInt64(reader.GetOrdinal("podcast_keep_cap"));
            Title = reader.GetString(reader.GetOrdinal("podcast_title"));
            Author = reader.GetString(reader.GetOrdinal("podcast_author"));
            Description = reader.GetString(reader.GetOrdinal("podcast_description"));
            rssUrl = reader.GetString(reader.GetOrdinal("podcast_rss_url"));
        }

        /* Class methods */
        public static List<Podcast> ListOfStoredPodcasts()
        {   
            List<Podcast> list = new List<Podcast>();
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("SELECT podcast_id FROM podcast ORDER BY podcast_title DESC", conn);
                q.Prepare();
                reader = q.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Podcast(reader.GetInt32(0)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (3)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
            return list;
        }

        private static int? PodcastIdForRssUrl(string rss)
        {
            IDbConnection conn = null;
            IDataReader reader = null;
            try
            {
                conn = Database.GetDbConnection();

                IDbCommand q = Database.GetDbCommand("SELECT podcast_id FROM podcast WHERE podcast_rss_url = @rss", conn);
                q.AddNamedParam("@rss", rss);
                q.Prepare();
                reader = q.ExecuteReader();

                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[PODCAST (3)] ERROR: " +  e);
            }
            finally
            {
                Database.Close(conn, reader);
            }
            return null;
        }
    }
}

