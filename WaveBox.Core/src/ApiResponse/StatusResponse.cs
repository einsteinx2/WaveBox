using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse {
    public class StatusResponse : IApiResponse {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("status")]
        public IDictionary<string, object> Status { get; set; }

        public StatusResponse(string error, IDictionary<string, object> status) {
            Error = error;
            Status = status;
        }
    }
}

