using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class PodcastContentResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("podcasts")]
		public List<Podcast> Podcasts { get; set; }

		[JsonProperty("episodes")]
		public List<PodcastEpisode> Episodes { get; set; }

		public PodcastContentResponse(string error, List<Podcast> podcasts)
		{
			Error = error;
			Podcasts = podcasts;
			Episodes = null;
		}

		public PodcastContentResponse(string error, Podcast podcast, List<PodcastEpisode> episodes)
		{
			Error = error;
			Podcasts = new List<Podcast> { podcast };
			Episodes = episodes;
		}
	}
}

