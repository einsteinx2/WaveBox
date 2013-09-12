using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class ExternalArtResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		public ExternalArtResponse(string error, string url)
		{
			Error = error;
			Url = url;
		}
	}
}

