using System;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	public class UsersApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		public UsersApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		public void Process()
		{
			try
			{
				string json = JsonConvert.SerializeObject(new UsersResponse("UsersApiHandler not yet implemented"), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error("[USERSAPI(1)] ERROR: " + e);
			}
		}

		private class UsersResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			public UsersResponse(string error)
			{
				Error = error;
			}
		}
	}
}

