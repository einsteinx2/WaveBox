using System;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.ApiHandler.Handlers
{
	public class LoginApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
				string json = JsonConvert.SerializeObject(new LoginResponse(null, User.SessionId), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e.ToString());
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

