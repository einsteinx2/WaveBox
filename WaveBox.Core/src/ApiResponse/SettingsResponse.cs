using System;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class SettingsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("settings")]
        public ServerSettingsData Settings { get; set; }

        public SettingsResponse(string error, ServerSettingsData settings) {
            Error = error;
            Settings = settings;
        }
    }
}

