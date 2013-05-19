using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler
{
	class SettingsApiHandler : IApiHandler
	{
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

				// Attempt to write settings
				bool success = false;
				try
				{
					success = Settings.WriteSettings(settingsJson);
				}
				catch (JsonException)
				{
					// Failure if invalid JSON provided
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("Invalid JSON", Settings.SettingsModel), Settings.JsonFormatting));
					return;
				}
				
				// If settings wrote successfully, return success object
				if (success)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Settings.SettingsModel), Settings.JsonFormatting));
				}
				else
				{
					// If no settings changed, report a 'harmless' error
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("No settings were changed", Settings.SettingsModel), Settings.JsonFormatting));
				}
			}
			else
			{
				// If no parameter provided, return settings
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
