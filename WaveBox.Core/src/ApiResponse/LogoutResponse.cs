using System;
using Newtonsoft.Json;

namespace WaveBox.Core
{
	public class LogoutResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("sessionId")]
		public string SessionId { get; set; }

		public LogoutResponse(string error, string sessionId)
		{
			Error = error;
			SessionId = sessionId;
		}
	}
}

