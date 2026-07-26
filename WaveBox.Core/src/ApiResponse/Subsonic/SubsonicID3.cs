using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    // ID3-flavored browsing DTOs (getArtists/getArtist/getAlbum).  WaveBox maps the Subsonic
    // "ID3 artist" concept onto AlbumArtist, which matches how tag-based clients expect
    // compilations to group.
    public class SubsonicArtistsID3 {
        [JsonPropertyName("ignoredArticles")]
        public string IgnoredArticles { get; set; } = "";

        [JsonPropertyName("index"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicIndexID3> Index { get; set; }
    }

    public class SubsonicIndexID3 {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicArtistID3> Artist { get; set; }
    }

    public class SubsonicArtistID3 {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("coverArt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CoverArt { get; set; }

        [JsonPropertyName("albumCount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AlbumCount { get; set; }

        [JsonPropertyName("starred"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Starred { get; set; }
    }

    public class SubsonicArtistWithAlbumsID3 : SubsonicArtistID3 {
        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicAlbumID3> Album { get; set; }
    }

    public class SubsonicAlbumID3 {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Artist { get; set; }

        [JsonPropertyName("artistId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ArtistId { get; set; }

        [JsonPropertyName("coverArt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CoverArt { get; set; }

        [JsonPropertyName("songCount")]
        public int SongCount { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("created"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Created { get; set; }

        [JsonPropertyName("year"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Year { get; set; }

        [JsonPropertyName("genre"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Genre { get; set; }

        [JsonPropertyName("starred"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Starred { get; set; }
    }

    public class SubsonicAlbumWithSongsID3 : SubsonicAlbumID3 {
        [JsonPropertyName("song"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicChild> Song { get; set; }
    }
}
