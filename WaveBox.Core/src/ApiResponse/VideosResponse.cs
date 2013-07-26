using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Core.ApiResponse
{
	public class VideosResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("videos")]
		public IList<Video> Videos { get; set; }

		public VideosResponse(string error, IList<Video> videos)
		{
			Error = error;
			Videos = videos;
		}
	}
}

