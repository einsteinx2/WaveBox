using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.Http;
using Newtonsoft.Json;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private string Err { get; set; }

		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor)
		{
			Processor = processor;
			Uri = uri;
			Err = "Invalid API call";
		}

		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor, string err)
		{
			Processor = processor;
			Uri = uri;
			Err = err;
		}

		public void Process()
		{
			logger.Info("[ERROR HANDLER]: " + Err);

			Dictionary<string, string> response = new Dictionary<string, string>();
			response["error"] = Err;

			string json = JsonConvert.SerializeObject(response, Settings.JsonFormatting);
			Processor.WriteJson(json);
		}
	}
}
