using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class TranscodeResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public TranscodeResponse(string error) {
            Error = error;
        }
    }
}

