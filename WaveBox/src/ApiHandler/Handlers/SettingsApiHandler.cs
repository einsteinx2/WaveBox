using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler
{
	class SettingsApiHandler : IApiHandler
	{		
		//private static Logger logger = LogManager.GetCurrentClassLogger();
		
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public SettingsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			if (Uri.Parameters.ContainsKey("settingsJson"))
			{
				// Take in settings in the JSON format (same as it is stored on disk) and pass it on to the Settings class for processing=
				string settingsJson = Uri.Parameters["settingsJson"];

				bool success = false;
				try
				{
					success = Settings.WriteSettings(settingsJson);
				}
				catch (JsonException)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("Invalid JSON", Settings.SettingsModel), Settings.JsonFormatting));
					return;
				}
				
				if (success)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Settings.SettingsModel), Settings.JsonFormatting));
				}
				else
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("No settings were changed", Settings.SettingsModel), Settings.JsonFormatting));
				}
			}
			else
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Settings.SettingsModel), Settings.JsonFormatting));
			}
		}
		
		private class SettingsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("settings")]
			public SettingsData Settings { get; set; }
			
			public SettingsResponse(string error, SettingsData settings)
			{
				Error = error;
				Settings = settings;
			}
		}
	}
}