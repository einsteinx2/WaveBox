using System;
using Newtonsoft.Json;

namespace WaveBox.Core
{
	public class LoginResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("sessionId")]
		public string SessionId { get; set; }

		public LoginResponse(string error, string sessionId)
		{
			Error = error;
			SessionId = sessionId;
		}
	}
}

