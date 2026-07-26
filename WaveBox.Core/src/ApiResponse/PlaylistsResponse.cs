using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class PlaylistsResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("playlists")]
        public IList<Playlist> Playlists { get; set; }

        [JsonPropertyName("mediaItems")]
        public IList<IMediaItem> MediaItems { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public PlaylistsResponse(string error, IList<Playlist> playlists, IList<IMediaItem> mediaItems, PairList<string, int> sectionPositions) {
            Error = error;
            Playlists = playlists;
            MediaItems = mediaItems;
            SectionPositions = sectionPositions;
        }
    }
}

