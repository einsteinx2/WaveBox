using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class ScrobbleResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("authUrl")]
		public string AuthUrl { get; set; }

		public ScrobbleResponse(string error = null, string authUrl = null)
		{
			Error = error;
			AuthUrl = authUrl;
		}
	}
}

