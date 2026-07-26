using System;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse {
    public class JukeboxStatus : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("currentIndex")]
        public int CurrentIndex { get; set; }

        [JsonPropertyName("progress")]
        public double Progress { get; set; }

        public JukeboxStatus(string error, string state, int currentIndex, double progress) {
            Error = error;
            State = state;
            CurrentIndex = currentIndex;
            Progress = progress;
        }
    }
}

