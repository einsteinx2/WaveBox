using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace PodcastParsing
{
    public class DownloadQueue
    {
        public int Count
        {
            get
            {
                return q.Count;
            }
        }

        private List<PodcastEpisode> q = new List<PodcastEpisode>();
        private Object listLock = new Object();

        public DownloadQueue()
        {
        }

        public void Enqueue(PodcastEpisode p)
        { 
            lock(listLock)
                q.Add(p);
        }

        public PodcastEpisode Dequeue()
        {
            PodcastEpisode temp;
            lock (listLock)
            {
                temp = q [0];
                q.RemoveAt(0);
            }

            return temp;
        }

        public PodcastEpisode CurrentItem()
        {
            if(q.Count > 0)
                return q[0];
            else return null;
        }

        public bool RemovePodcast(int podcastId)
        {
            lock (listLock)
            {
                for (int i = 0; i < q.Count; i++)
                {
                    if(q[i].PodcastId == podcastId)
                    {
                        // if this is at the head of the queue and is being downloaded
                        if(i == 0 && PodcastEpisode.webClient.IsBusy)
                        {
                            // cancel the download
                            PodcastEpisode.webClient.CancelAsync();

                            // clean up any partially downloaded file
                            if(File.Exists(q[i].FilePath)) File.Delete(q[i].FilePath);
                            Console.WriteLine("Download canceled");

                            // remove the item from the queue
                            q.Remove(q[i]);

                            // start the queue back up
                            Dequeue();
                            CurrentItem().StartDownload();
                        }
                        // remove the item from the queue
                        else q.Remove(q[i]);

                        // we did remove an item from the queue
                        return true;
                    }
                }
                // we did not remove an item from the queue
                return false;
            }
        }
    }
}

