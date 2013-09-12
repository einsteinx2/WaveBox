using System;
using Newtonsoft.Json;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class SettingsResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("settings")]
		public ServerSettingsData Settings { get; set; }

		public SettingsResponse(string error, ServerSettingsData settings)
		{
			Error = error;
			Settings = settings;
		}
	}
}

