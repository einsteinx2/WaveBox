using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using WaveBox.OperationQueue;
using NLog;

namespace WaveBox.PodcastManagement
{
    /// <summary>
    /// The podcast download queue.  This class wraps a list and implements some queue functions.  The reason for doing this is that we needed the queue to be mutable in other ways than pushing and popping.
    /// </summary>
    public static class DownloadQueue
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

        public static int Count { get { return q.Count; } }

        // this really doesn't belong here, but the current operation queue model says that it should be.
        public static DelayedOperationQueue FeedChecks = new DelayedOperationQueue();

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
                    var shouldAdd = true;
                    foreach(PodcastEpisode q_ep in q)
                    {
                        if (q_ep.Title == ep.Title && q_ep.Author == ep.Author)
                            shouldAdd = false;
                    }
                    if(shouldAdd)
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
                            logger.Info("Download canceled");

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

        public static void CancelAll()
        {
            int countBeforeRemoval = q.Count;

            for (int i = 0; i < countBeforeRemoval; i++)
            {
                // if this is at the head of the queue and is being downloaded
                if(i == 0 && webClient.IsBusy)
                {
                    // cancel the download
                    webClient.CancelAsync();
                    
                    // clean up any partially downloaded file
                    if(File.Exists(q[i].FilePath)) File.Delete(q[i].FilePath);
                    logger.Info("Download canceled");
                    
                    // remove the item from the queue
                    Dequeue();
                }
                // remove the item from the queue
                else q.Remove(q[i]);
            }
        }

        public static void StartDownload(PodcastEpisode ep)
        {
            var sw = new Stopwatch();
            webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => 
            {
                if (contentLength == 0)
                    contentLength = e.TotalBytesToReceive;
                //logger.Info(ep.Title + ": " + ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100 + "%");
                totalBytesRead = e.BytesReceived;
            });

            webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler((sender, e) => 
            {
                ep.AddToDatabase();
                sw.Stop();
                logger.Info("[PODCASTMANAGEMENT] Finished downloading {0} [ {1}, {2}Mbps avg ]", ep.Title, sw.ElapsedMilliseconds / 1000, ((double)totalBytesRead / (double)131072) / (sw.ElapsedMilliseconds / 1000));

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
            logger.Info("[PODCASTMANAGEMENT] Started downloading {0}", ep.Title);
            sw.Start();
        }

        public static double DownloadProgress()
        {
            return (double)totalBytesRead / (double)contentLength;
        }
    }
}

