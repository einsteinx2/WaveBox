using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class ScrobbleResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("authUrl")]
        public string AuthUrl { get; set; }

        public ScrobbleResponse(string error = null, string authUrl = null) {
            Error = error;
            AuthUrl = authUrl;
        }
    }
}

