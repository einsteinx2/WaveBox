using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class TranscodeHlsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		public TranscodeHlsResponse(string error)
		{
			Error = error;
		}
	}
}

