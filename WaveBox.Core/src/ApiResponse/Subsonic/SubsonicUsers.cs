using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    public class SubsonicUsers {
        [JsonPropertyName("user"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicUser> User { get; set; }
    }

    // WaveBox roles are coarse (Test < Guest < User < Admin), so the fine-grained Subsonic role
    // flags are derived: User-level grants the everyday roles, Admin grants administration.
    // Features WaveBox doesn't have (jukebox, sharing, podcasts, uploads) are always false.
    public class SubsonicUser {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("email"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Email { get; set; }

        [JsonPropertyName("scrobblingEnabled")]
        public bool ScrobblingEnabled { get; set; }

        [JsonPropertyName("adminRole")]
        public bool AdminRole { get; set; }

        [JsonPropertyName("settingsRole")]
        public bool SettingsRole { get; set; }

        [JsonPropertyName("downloadRole")]
        public bool DownloadRole { get; set; }

        [JsonPropertyName("uploadRole")]
        public bool UploadRole { get; set; }

        [JsonPropertyName("playlistRole")]
        public bool PlaylistRole { get; set; }

        [JsonPropertyName("coverArtRole")]
        public bool CoverArtRole { get; set; }

        [JsonPropertyName("commentRole")]
        public bool CommentRole { get; set; }

        [JsonPropertyName("podcastRole")]
        public bool PodcastRole { get; set; }

        [JsonPropertyName("streamRole")]
        public bool StreamRole { get; set; }

        [JsonPropertyName("jukeboxRole")]
        public bool JukeboxRole { get; set; }

        [JsonPropertyName("shareRole")]
        public bool ShareRole { get; set; }

        [JsonPropertyName("videoConversionRole")]
        public bool VideoConversionRole { get; set; }

        [JsonPropertyName("folder"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<int> Folder { get; set; }
    }
}
