using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    public class SubsonicPlaylists {
        [JsonPropertyName("playlist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicPlaylist> Playlist { get; set; }
    }

    // WaveBox playlists have no owner or visibility columns; owner is synthesized as the
    // requesting user and playlists are reported as public (every WaveBox user sees them all).
    public class SubsonicPlaylist {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("owner"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Owner { get; set; }

        [JsonPropertyName("public")]
        public bool Public { get; set; } = true;

        [JsonPropertyName("songCount")]
        public int SongCount { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("created"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Created { get; set; }

        [JsonPropertyName("changed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Changed { get; set; }
    }

    public class SubsonicPlaylistWithSongs : SubsonicPlaylist {
        [JsonPropertyName("entry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Entry { get; set; }
    }
}
