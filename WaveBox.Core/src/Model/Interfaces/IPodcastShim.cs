using System;
using System.Collections.Generic;

namespace WaveBox.Model
{
	public interface IPodcastShim
	{
		void Enqueue(List<PodcastEpisode> episodes);
		void RemovePodcast(long podcastId);
	}
}

