using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class ErrorResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public ErrorResponse(string error) {
            Error = error;
        }
    }
}

