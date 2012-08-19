using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Http;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public StatusApiHandler(UriWrapper uri, IHttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			Console.WriteLine("[STATUS] OK!");
			
			try
			{
				string json = JsonConvert.SerializeObject(new StatusResponse(null, "1"), Formatting.None);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[STATUS(1)] ERROR: " + e.ToString());
			}
		}
	}

	class StatusResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		public StatusResponse(string error, string version)
		{
			Error = error;
			Version = version;
		}
	}
}
