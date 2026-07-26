using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class StreamResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public StreamResponse(string error) {
            Error = error;
        }
    }
}

