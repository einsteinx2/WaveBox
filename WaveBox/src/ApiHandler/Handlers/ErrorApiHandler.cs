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

		/// <summary>
		/// Constructor for ErrorApiHandler class
		/// </summary>
		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor)
		{
			Processor = processor;
			Uri = uri;
			Err = "Invalid API call";
		}

		/// <summary>
		/// Overload constructor for ErrorApiHandler class (custom error message)
		/// </summary>
		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor, string err)
		{
			Processor = processor;
			Uri = uri;
			Err = err;
		}

		/// <summary>
		/// Process logs the error, creates a JSON response, and sends
		/// it back to the user on bad API call
		/// </summary>
		public void Process()
		{
			logger.Info("[ERRORAPI(1)]: " + Err);

			Dictionary<string, string> response = new Dictionary<string, string>();
			response["error"] = Err;

			string json = JsonConvert.SerializeObject(response, Settings.JsonFormatting);
			Processor.WriteJson(json);
		}
	}
}
