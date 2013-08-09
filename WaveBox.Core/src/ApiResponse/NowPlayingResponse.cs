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
		public IList<Dictionary<string, object>> NowPlaying { get; set; }

		public NowPlayingResponse(string error, IList<Dictionary<string, object>> nowPlaying)
		{
			Error = error;
			NowPlaying = nowPlaying;
		}
	}
}
