using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class StatsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public StatsResponse(string error) {
            Error = error;
        }
    }
}

