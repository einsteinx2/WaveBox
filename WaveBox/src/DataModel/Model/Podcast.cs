using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using WaveBox.DataModel.Singletons;

namespace PodcastParsing
{
    public class Podcast
    {
        /* IVars */
        private string rssUrl;
        XmlDocument doc;
        XmlNamespaceManager mgr;

        /* Properties */
        public int? PodcastId { get; set; }
        public int? ArtId { get; set; }
        public int EpisodeKeepCap { get; set; } 
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }

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

            Console.WriteLine(Title + "\r\n " + Author + "\r\n " + Description + "\r\n\r\n");
        }

        /* Instance methods */
        public void AddToDatabase()
        {
        }

        public void DownloadNewEpisodes()
        {
            AddToDatabase();

            var current = ListOfCurrentEpisodes();
            var stored = ListOfStoredEpisodes();
            var newEps = new List<PodcastEpisode>();

            // get new episodes
            foreach (var currentEp in current)
            {
                bool epIsNew = true;
                foreach (var storedEp in stored)
                {
                    if (storedEp.Title == currentEp.Title)
                        epIsNew = false;
                }

                if (epIsNew)
                {
                    newEps.Add(currentEp);
                }
            }

            if (stored.Count == EpisodeKeepCap)
            {
                DeleteOldEpisodes(newEps.Count);
            } 

            foreach(var episode in newEps)
            {
                // episode will be added to database when it has successfully completed downloading
                episode.Download();
            }
        }

        private void DeleteOldEpisodes(int count)
        {
        }

        public List<PodcastEpisode> ListOfCurrentEpisodes()
        {            
            XmlNodeList xmlList;
            xmlList = doc.SelectNodes("//item");
            var list = new List<PodcastEpisode>();

            // Make sure we don't try to add more episodes than there actually are.
            int j = EpisodeKeepCap <= xmlList.Count ? EpisodeKeepCap : list.Count;
            for(int i = 0; i < j; i++)
            {
                list.Add(new PodcastEpisode(xmlList.Item(i), mgr, PodcastId));
            }
            return list;
        }

        public List<PodcastEpisode> ListOfStoredEpisodes()
        {
            var list = new List<PodcastEpisode>();
            return list;
        }
        public PodcastEpisode MostRecentEpisode()
        {
            return new PodcastEpisode();
        }

        /* Class methods */
        public static List<Podcast> AvailablePodcasts()
        {
            return new List<Podcast>();
        }
    }
}

