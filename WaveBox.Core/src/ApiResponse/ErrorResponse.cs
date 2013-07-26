using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class ErrorResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		public ErrorResponse(string error)
		{
			Error = error;
		}
	}
}

