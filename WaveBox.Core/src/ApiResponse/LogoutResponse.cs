using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse {
    public class LogoutResponse : IApiResponse {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        public LogoutResponse(string error, string sessionId) {
            Error = error;
            SessionId = sessionId;
        }
    }
}

