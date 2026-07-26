using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    public class SubsonicMusicFolders {
        [JsonPropertyName("musicFolder"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicMusicFolder> MusicFolder { get; set; }
    }

    public class SubsonicMusicFolder {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }
    }

    public class SubsonicIndexes {
        // Milliseconds since epoch (Subsonic quirk: this one field is ms, durations are seconds)
        [JsonPropertyName("lastModified")]
        public long LastModified { get; set; }

        [JsonPropertyName("ignoredArticles")]
        public string IgnoredArticles { get; set; } = "";

        [JsonPropertyName("index"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicIndex> Index { get; set; }

        // Loose media files directly inside a music folder root
        [JsonPropertyName("child"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Child { get; set; }
    }

    public class SubsonicIndex {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicIndexArtist> Artist { get; set; }
    }

    // Folder-style "artist" entry used by getIndexes, search2, and getStarred: just an id
    // (a folder id in getIndexes) and a display name.
    public class SubsonicIndexArtist {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("starred"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Starred { get; set; }
    }

    public class SubsonicDirectory {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("parent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Parent { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("child"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Child { get; set; }
    }

    // The shared Subsonic media/directory-entry DTO ("Child" in the Subsonic schema), used by
    // getMusicDirectory, getSong, album/song lists, search results, playlists, and now playing.
    public class SubsonicChild {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("parent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Parent { get; set; }

        [JsonPropertyName("isDir")]
        public bool IsDir { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Album { get; set; }

        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Artist { get; set; }

        [JsonPropertyName("track"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Track { get; set; }

        [JsonPropertyName("year"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Year { get; set; }

        [JsonPropertyName("genre"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Genre { get; set; }

        [JsonPropertyName("coverArt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CoverArt { get; set; }

        [JsonPropertyName("size"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Size { get; set; }

        [JsonPropertyName("contentType"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ContentType { get; set; }

        [JsonPropertyName("suffix"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Suffix { get; set; }

        [JsonPropertyName("duration"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Duration { get; set; }

        [JsonPropertyName("bitRate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BitRate { get; set; }

        [JsonPropertyName("path"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Path { get; set; }

        [JsonPropertyName("isVideo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsVideo { get; set; }

        [JsonPropertyName("discNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DiscNumber { get; set; }

        [JsonPropertyName("created"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Created { get; set; }

        [JsonPropertyName("starred"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Starred { get; set; }

        [JsonPropertyName("albumId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AlbumId { get; set; }

        [JsonPropertyName("artistId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ArtistId { get; set; }

        [JsonPropertyName("type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; }
    }

    public class SubsonicGenres {
        [JsonPropertyName("genre"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicGenre> Genre { get; set; }
    }

    public class SubsonicGenre {
        [JsonPropertyName("songCount")]
        public int SongCount { get; set; }

        [JsonPropertyName("albumCount")]
        public int AlbumCount { get; set; }

        // The genre name: XML text content, JSON "value" property (Subsonic convention)
        [JsonPropertyName("value"), SubsonicXmlText]
        public string Value { get; set; }
    }

    public class SubsonicVideos {
        [JsonPropertyName("video"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Video { get; set; }
    }

    public class SubsonicLyrics {
        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Artist { get; set; }

        [JsonPropertyName("title"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Title { get; set; }

        [JsonPropertyName("value"), SubsonicXmlText, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Value { get; set; }
    }
}
