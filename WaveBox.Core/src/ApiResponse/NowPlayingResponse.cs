using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class NowPlayingResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("nowPlaying")]
		public IList<NowPlaying> NowPlaying { get; set; }

		public NowPlayingResponse(string error, IList<NowPlaying> nowPlaying)
		{
			Error = error;
			NowPlaying = nowPlaying;
		}
	}
}
