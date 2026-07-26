using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class ArtistsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("artists")]
        public IList<Artist> Artists { get; set; }

        [JsonPropertyName("albums")]
        public IList<Album> Albums { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("counts")]
        public Dictionary<string, int> Counts { get; set; }

        [JsonPropertyName("lastfmInfo")]
        public string LastfmInfo { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public ArtistsResponse(string error, IList<Artist> artists, IList<Album> albums, IList<Song> songs, Dictionary<string, int> counts, string lastfmInfo, PairList<string, int> sectionPositions) {
            Error = error;
            Artists = artists;
            Songs = songs;
            Albums = albums;
            Counts = counts;
            LastfmInfo = lastfmInfo;
            SectionPositions = sectionPositions;
        }
    }
}

