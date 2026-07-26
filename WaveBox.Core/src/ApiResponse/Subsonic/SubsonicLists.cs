using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    public class SubsonicAlbumList {
        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Album { get; set; }
    }

    public class SubsonicAlbumList2 {
        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicAlbumID3> Album { get; set; }
    }

    // Shared by getRandomSongs (randomSongs) and getSongsByGenre (songsByGenre)
    public class SubsonicSongs {
        [JsonPropertyName("song"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Song { get; set; }
    }

    public class SubsonicNowPlaying {
        [JsonPropertyName("entry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicNowPlayingEntry> Entry { get; set; }
    }

    public class SubsonicNowPlayingEntry : SubsonicChild {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("minutesAgo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinutesAgo { get; set; }

        [JsonPropertyName("playerId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PlayerId { get; set; }

        [JsonPropertyName("playerName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PlayerName { get; set; }
    }

    public class SubsonicStarred {
        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicIndexArtist> Artist { get; set; }

        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Album { get; set; }

        [JsonPropertyName("song"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Song { get; set; }
    }

    public class SubsonicStarred2 {
        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicArtistID3> Artist { get; set; }

        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicAlbumID3> Album { get; set; }

        [JsonPropertyName("song"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Song { get; set; }
    }
}
