using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Core;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.ApiResponse;

namespace WaveBox.ApiHandler
{
	class SettingsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public SettingsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			if (Uri.Parameters.ContainsKey("json"))
			{
				// Take in settings in the JSON format (same as it is stored on disk) and pass it on to the Settings class for processing=
				string json = HttpUtility.UrlDecode(Uri.Parameters["json"]);
				if (logger.IsInfoEnabled) logger.Info("Received settings JSON: " + json);

				// Attempt to write settings
				bool success = false;
				try
				{
					success = Injection.Kernel.Get<IServerSettings>().WriteSettings(json);
					Injection.Kernel.Get<IServerSettings>().Reload();
				}
				catch (JsonException)
				{
					// Failure if invalid JSON provided
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("Invalid JSON", Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}
				
				// If settings wrote successfully, return success object
				if (success)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
				}
				else
				{
					// If no settings changed, report a 'harmless' error
					Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("No settings were changed", Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
				}
			}
			else
			{
				// If no parameter provided, return settings
				Processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
			}
		}
	}
}
