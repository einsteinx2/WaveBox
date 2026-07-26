using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class AlbumsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("albums")]
        public IList<Album> Albums { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public AlbumsResponse(string error, IList<Album> albums, IList<Song> songs, PairList<string, int> sectionPositions) {
            Error = error;
            Albums = albums;
            Songs = songs;
            SectionPositions = sectionPositions;
        }
    }
}

