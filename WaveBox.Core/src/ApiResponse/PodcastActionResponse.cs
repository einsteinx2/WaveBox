using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class PodcastActionResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("success")]
		public bool Success { get; set; }

		public PodcastActionResponse(string error, bool success)
		{
			Error = error;
			Success = success;
		}
	}
}

