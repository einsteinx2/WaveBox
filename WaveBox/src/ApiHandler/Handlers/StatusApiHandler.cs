using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.HttpServer;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public StatusApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			Console.WriteLine("Status: ok!");
			string json = JsonConvert.SerializeObject(new StatusResponse(null, "1"), Formatting.None);
			Processor.OutputStream.Write(json);
		}
	}

	class StatusResponse
	{
		public string Error { get; set; }
		public string Version { get; set; }

		public StatusResponse(string error, string version)
		{
			Error = error;
			Version = version;
		}
	}
}
