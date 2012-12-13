using System;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	public class LoginApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructor for LoginApiHandler
		/// </summary>
		public LoginApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process returns a new session key for this user upon successful login
		/// <summary>
		public void Process()
		{
			try
			{
				string json = JsonConvert.SerializeObject(new LoginResponse(null, User.SessionId), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error("[LOGINAPI(1)] ERROR: " + e);
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

