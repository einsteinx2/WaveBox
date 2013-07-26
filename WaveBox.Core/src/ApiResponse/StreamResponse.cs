using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class StreamResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		public StreamResponse(string error)
		{
			Error = error;
		}
	}
}

