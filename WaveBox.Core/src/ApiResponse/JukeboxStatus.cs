using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse {
    public class JukeboxStatus : IApiResponse {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("currentIndex")]
        public int CurrentIndex { get; set; }

        [JsonProperty("progress")]
        public double Progress { get; set; }

        public JukeboxStatus(string error, string state, int currentIndex, double progress) {
            Error = error;
            State = state;
            CurrentIndex = currentIndex;
            Progress = progress;
        }
    }
}

