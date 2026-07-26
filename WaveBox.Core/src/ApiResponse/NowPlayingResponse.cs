using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class NowPlayingResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("nowPlaying")]
        public IList<NowPlaying> NowPlaying { get; set; }

        public NowPlayingResponse(string error, IList<NowPlaying> nowPlaying) {
            Error = error;
            NowPlaying = nowPlaying;
        }
    }
}
