using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse {
    public class StatusResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("status")]
        public IDictionary<string, object> Status { get; set; }

        public StatusResponse(string error, IDictionary<string, object> status) {
            Error = error;
            Status = status;
        }
    }
}

