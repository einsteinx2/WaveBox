using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Http;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
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
			Console.WriteLine("[ERROR HANDLER]: " + Err);

			var response = new Dictionary<string, string>();
			response["error"] = Err;

			var json = JsonConvert.SerializeObject(response, Formatting.None);
			Processor.WriteJson(json);
		}
	}
}
