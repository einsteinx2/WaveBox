using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class SearchResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("artists")]
        public IList<Artist> Artists { get; set; }

        [JsonPropertyName("albumArtists")]
        public IList<AlbumArtist> AlbumArtists { get; set; }

        [JsonPropertyName("albums")]
        public IList<Album> Albums { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("videos")]
        public IList<Video> Videos { get; set; }

        public SearchResponse(string error, IList<Artist> artists, IList<AlbumArtist> albumArtists, IList<Album> albums, IList<Song> songs, IList<Video> videos) {
            Error = error;
            Artists = artists;
            AlbumArtists = albumArtists;
            Albums = albums;
            Songs = songs;
            Videos = videos;
        }
    }
}

