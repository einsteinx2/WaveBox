using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class LogoutResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        public LogoutResponse(string error, string sessionId) {
            Error = error;
            SessionId = sessionId;
        }
    }
}

