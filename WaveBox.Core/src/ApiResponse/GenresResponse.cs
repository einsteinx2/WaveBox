using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class GenresResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("genres")]
        public IList<Genre> Genres { get; set; }

        [JsonPropertyName("folders")]
        public IList<Folder> Folders { get; set; }

        [JsonPropertyName("artists")]
        public IList<Artist> Artists { get; set; }

        [JsonPropertyName("albums")]
        public IList<Album> Albums { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public GenresResponse(string error, IList<Genre> genres, IList<Folder> folders, IList<Artist> artists, IList<Album> albums, IList<Song> songs, PairList<string, int> sectionPositions) {
            Error = error;
            Genres = genres;
            Folders = folders;
            Artists = artists;
            Albums = albums;
            Songs = songs;
            SectionPositions = sectionPositions;
        }
    }
}

