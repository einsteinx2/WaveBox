using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class PodcastActionResponse
	{
		[JsonProperty("error")]
		public bool Success { get; set; }

		[JsonProperty("success")]
		public string ErrorMessage { get; set; }

		public PodcastActionResponse(string errorMessage, bool success)
		{
			ErrorMessage = errorMessage;
			Success = success;
		}
	}
}

