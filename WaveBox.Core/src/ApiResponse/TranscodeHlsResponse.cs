using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse {
    public class TranscodeHlsResponse : IApiResponse {
        [JsonProperty("error")]
        public string Error { get; set; }

        public TranscodeHlsResponse(string error) {
            Error = error;
        }
    }
}

