using System;
using WaveBox.Service.Services.Cron;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox
{
	// Temporary during refactoring
	public class PodcastShim : IPodcastShim
	{
		public PodcastShim()
		{
		}

		public void Enqueue(List<PodcastEpisode> episodes)
		{
			DownloadQueue.Enqueue(episodes);
		}

		public void RemovePodcast(long podcastId)
		{
			DownloadQueue.RemovePodcast(podcastId);
		}
	}
}

