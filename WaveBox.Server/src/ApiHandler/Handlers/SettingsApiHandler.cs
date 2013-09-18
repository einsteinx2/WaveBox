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

		// Standard permissions
		public bool CheckPermission(User user, string action)
		{
			switch (action)
			{
				// Write
				case "update":
					return user.HasPermission(Role.User);
				// Read
				case "read":
				default:
					return user.HasPermission(Role.Test);
			}

			return false;
		}

		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Read out settings
			if (uri.Action == "read" || uri.Action == null)
			{
				// If no parameter provided, return settings
				processor.WriteJson(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel));
				return;
			}

			// Check for required JSON parameter
			if (!uri.Parameters.ContainsKey("json"))
			{
				processor.WriteJson(new SettingsResponse("Missing required parameter 'json'", null));
				return;
			}

			// Update settings
			if (uri.Action == "update")
			{
				// Take in settings in the JSON format (same as it is stored on disk),
				// pass it on to the Settings class for processing
				string json = HttpUtility.UrlDecode(uri.Parameters["json"]);

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
					processor.WriteJson(new SettingsResponse("Invalid JSON", null));
					return;
				}

				// If settings fail to write, report error
				if (!success)
				{
					processor.WriteJson(new SettingsResponse("Settings could not be changed", null));
					return;
				}

				// If settings wrote successfully, return success
				processor.WriteJson(new SettingsResponse(null, Injection.Kernel.Get<IServerSettings>().SettingsModel));
				return;
			}

			// Invalid action
			processor.WriteJson(new SettingsResponse("Invalid action", null));
			return;
		}
	}
}
