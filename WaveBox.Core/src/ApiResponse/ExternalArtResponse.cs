using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class ExternalArtResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        public ExternalArtResponse(string error, string url) {
            Error = error;
            Url = url;
        }
    }
}

