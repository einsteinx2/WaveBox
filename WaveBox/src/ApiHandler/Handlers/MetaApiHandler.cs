using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class MetaApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for MetaApiHandler
		/// </summary>
		public MetaApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a serialized list of informationa about WaveBox in JSON format
		/// </summary>
		public void Process()
		{
			// Serialize MetaResponse object, write to HTTP response
			string json = JsonConvert.SerializeObject(new MetaResponse(null, "WaveBox-20121206-alpha"), Settings.JsonFormatting);
			Processor.WriteJson(json);
		}

		private class MetaResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("version")]
			public string Version { get; set; }

			public MetaResponse(string error, string version)
			{
				Error = error;
				Version = version;
			}
		}
	}
}
