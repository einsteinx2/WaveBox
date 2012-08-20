using System;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	public class LoginApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		public LoginApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		public void Process()
		{
			try
			{
				string json = JsonConvert.SerializeObject(new LoginResponse(null, User.SessionId), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[ALBUMSAPI(1)] ERROR: " + e.ToString());
			}
		}

		private class LoginResponse
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
}

