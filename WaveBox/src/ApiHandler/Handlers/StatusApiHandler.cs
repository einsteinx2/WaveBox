using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Http;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;
using WaveBox.DataModel.Model;

namespace WaveBox.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public StatusApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			Console.WriteLine("[STATUS] OK!");
			
			try
			{
				IDictionary<string, string> status = new Dictionary<string, string>();
				status["version"] = "1";
				status["lastQueryLogId"] = Database.LastQueryLogId().ToString();

				string json = JsonConvert.SerializeObject(new StatusResponse(null, status), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[STATUS(1)] ERROR: " + e);
			}
		}

		private class StatusResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("status")]
			public IDictionary<string, string> Status { get; set; }

			public StatusResponse(string error, IDictionary<string, string> status)
			{
				Error = error;
				Status = status;
			}
		}
	}
}
