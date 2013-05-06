using System;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	public class UsersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructors for UsersApiHandler
		/// </summary>
		public UsersApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process allows the modification of users and their properties
		/// </summary>
		public void Process()
		{
			try
			{
				string json = JsonConvert.SerializeObject(new UsersResponse("UsersApiHandler not yet implemented"), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
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

