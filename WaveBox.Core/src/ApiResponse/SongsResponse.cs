using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class SongsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public SongsResponse(string error, IList<Song> songs, PairList<string, int> sectionPositions) {
            Error = error;
            Songs = songs;
            SectionPositions = sectionPositions;
        }
    }
}

