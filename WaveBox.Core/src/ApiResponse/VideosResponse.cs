using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class VideosResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("videos")]
        public IList<Video> Videos { get; set; }

        public VideosResponse(string error, IList<Video> videos) {
            Error = error;
            Videos = videos;
        }
    }
}

