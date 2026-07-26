using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class TranscodeHlsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public TranscodeHlsResponse(string error) {
            Error = error;
        }
    }
}

