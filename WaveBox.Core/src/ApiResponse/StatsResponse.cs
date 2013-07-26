using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class StatsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		public StatsResponse(string error)
		{
			Error = error;
		}
	}
}

