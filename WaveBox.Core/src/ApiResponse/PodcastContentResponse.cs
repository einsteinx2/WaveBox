using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class PodcastContentResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("podcasts")]
		public IList<Podcast> Podcasts { get; set; }

		[JsonProperty("episodes")]
		public IList<PodcastEpisode> Episodes { get; set; }

		public PodcastContentResponse(string error, IList<Podcast> podcasts)
		{
			Error = error;
			Podcasts = podcasts;
			Episodes = null;
		}

		public PodcastContentResponse(string error, Podcast podcast, IList<PodcastEpisode> episodes)
		{
			Error = error;
			Podcasts = new List<Podcast> { podcast };
			Episodes = episodes;
		}
	}
}

