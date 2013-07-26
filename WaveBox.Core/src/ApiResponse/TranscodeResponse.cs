using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class TranscodeResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		public TranscodeResponse(string error)
		{
			Error = error;
		}
	}
}

