using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace WaveBox.PodcastManagement
{
    /// <summary>
    /// The podcast download queue.  This class wraps a list and implements some queue functions.  The reason for doing this is that we needed the queue to be mutable in other ways than pushing and popping.
    /// </summary>
    public static class DownloadQueue
    {
        public static int Count
        {
            get
            {
                return q.Count;
            }
        }

        private static List<PodcastEpisode> q = new List<PodcastEpisode>();
        private static Object listLock = new Object();
        private static WebClient webClient = new WebClient();
        private static long contentLength, totalBytesRead;

        public static void Enqueue(PodcastEpisode p)
        { 
            lock(listLock)
                q.Add(p);

            if (!webClient.IsBusy)
            {
                StartDownload(CurrentItem());
            }
        }

        public static void Enqueue(List<PodcastEpisode> p)
        {
            if(p.Count == 0) return;

            lock (listLock)
            {
                foreach (PodcastEpisode ep in p)
                {
                    q.Add(ep);
                }
            }

            if (!webClient.IsBusy)
            {
                StartDownload(CurrentItem());
            }
        }

        public static PodcastEpisode Dequeue()
        {
            PodcastEpisode temp;
            lock (listLock)
            {
                temp = q[0];
                q.RemoveAt(0);
            }

            return temp;
        }

        public static PodcastEpisode CurrentItem()
        {
            if(q.Count > 0)
                return q[0];
            else return null;
        }


        public static bool RemovePodcast(long podcastId)
        {
            // if there's nothing in the queue, this is a nonsensical request.
            if(q.Count == 0) return false;

            string previouslyDownloadingTitle = null;
            if(CurrentItem() != null)
                previouslyDownloadingTitle = CurrentItem().Title;

            bool didRemoveItem = false;
            lock (listLock)
            {
                int countBeforeRemoval = q.Count;
                int index = 0;

                for (int i = 0; i < countBeforeRemoval; i++)
                {
                    if(q[index].PodcastId == podcastId)
                    {
                        // if this is at the head of the queue and is being downloaded
                        if(index == 0 && webClient.IsBusy)
                        {
                            // cancel the download
                            webClient.CancelAsync();

                            // clean up any partially downloaded file
                            if(File.Exists(q[index].FilePath)) File.Delete(q[index].FilePath);
                            Console.WriteLine("Download canceled");

                            // remove the item from the queue
                            Dequeue();
                        }
                        // remove the item from the queue
                        else q.Remove(q[index]);

                        // we did remove an item from the queue
                        didRemoveItem = true;
                    }

                    // If this isn't the droid we're looking for, increment the index.  We should leave that one alone and
                    // look at the next one.
                    else index++;
                }

                // If there are still things to be downloaded after we remove this podcast, restart the queue.
                if(CurrentItem() != null && previouslyDownloadingTitle != null && previouslyDownloadingTitle != CurrentItem().Title)
                    StartDownload(CurrentItem());

                return didRemoveItem;
            }
        }

        public static void StartDownload(PodcastEpisode ep)
        {
            webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => 
            {
                if (contentLength == 0)
                    contentLength = e.TotalBytesToReceive;
                Console.WriteLine(ep.Title + ": " + ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100 + "%");
                totalBytesRead = e.BytesReceived;
            });

            webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler((sender, e) => 
            {
                ep.AddToDatabase();
                if(DownloadQueue.Count > 0)
                {
                    webClient.CancelAsync();
                    DownloadQueue.Dequeue();
                    if(DownloadQueue.CurrentItem() != null)
                        StartDownload(CurrentItem());
                }
            });

            Uri uri = new Uri(ep.MediaUrl);
            string[] fns = ep.MediaUrl.Split('/');
            string fn = fns[fns.Length - 1];
            Podcast pc = new Podcast(ep.PodcastId);
            ep.FilePath = Podcast.PodcastMediaDirectory + Path.DirectorySeparatorChar + pc.Title + Path.DirectorySeparatorChar + fn;

            webClient.DownloadFileAsync(uri, ep.FilePath);
        }

        public static double DownloadProgress()
        {
            return (double)totalBytesRead / (double)contentLength;
        }
    }
}

