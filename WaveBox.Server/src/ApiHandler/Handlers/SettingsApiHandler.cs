using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.ApiResponse;

namespace WaveBox.ApiHandler
{
	class SettingsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "settings"; } }

		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			if (uri.Parameters.ContainsKey("json"))
			{
				// Take in settings in the JSON format (same as it is stored on disk) and pass it on to the Settings class for processing=
				string json = HttpUtility.UrlDecode(uri.Parameters["json"]);
				logger.IfInfo("Received settings JSON: " + json);

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
					processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("Invalid JSON", Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}

				// If settings wrote successfully, return success object
				if (success)
				{
					processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
				}
				else
				{
					// If no settings changed, report a 'harmless' error
					processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse("No settings were changed", Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
				}
			}
			else
			{
				// If no parameter provided, return settings
				processor.WriteJson(JsonConvert.SerializeObject(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
			}
		}
	}
}
