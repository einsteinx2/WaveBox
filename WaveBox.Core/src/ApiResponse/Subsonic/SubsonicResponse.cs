using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    // Marks the DTO property whose value is written as XML element text content
    // (genre names, lyrics bodies) instead of an XML attribute.  JSON renders it
    // as a regular "value" property, matching the Subsonic JSON convention.
    [AttributeUsage(AttributeTargets.Property)]
    public class SubsonicXmlTextAttribute : Attribute {
    }

    // The one serialization root for the whole Subsonic surface.  Every response, success or
    // error, is this envelope with exactly one payload property set on the body.
    public class SubsonicResponse {
        [JsonPropertyName("subsonic-response")]
        public SubsonicResponseBody Body { get; set; }

        public SubsonicResponse() {
        }

        public SubsonicResponse(SubsonicResponseBody body) {
            Body = body;
        }
    }

    public class SubsonicResponseBody {
        // Highest Subsonic API version whose endpoints are covered here
        public const string ApiVersion = "1.16.1";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("version")]
        public string Version { get; set; } = ApiVersion;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "WaveBox";

        [JsonPropertyName("serverVersion")]
        public string ServerVersion { get; set; }

        [JsonPropertyName("openSubsonic")]
        public bool OpenSubsonic { get; set; } = true;

        // Exactly one of the following is non-null per response; nulls are suppressed so the
        // envelope stays clean.  XML renders each as a child element of <subsonic-response>.

        [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicError Error { get; set; }

        [JsonPropertyName("license"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicLicense License { get; set; }

        [JsonPropertyName("openSubsonicExtensions"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<SubsonicExtension> OpenSubsonicExtensions { get; set; }

        [JsonPropertyName("tokenInfo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicTokenInfo TokenInfo { get; set; }

        [JsonPropertyName("scanStatus"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicScanStatus ScanStatus { get; set; }

        [JsonPropertyName("musicFolders"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicMusicFolders MusicFolders { get; set; }

        [JsonPropertyName("indexes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicIndexes Indexes { get; set; }

        [JsonPropertyName("directory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicDirectory Directory { get; set; }

        [JsonPropertyName("genres"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicGenres Genres { get; set; }

        [JsonPropertyName("artists"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicArtistsID3 Artists { get; set; }

        [JsonPropertyName("artist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicArtistWithAlbumsID3 Artist { get; set; }

        [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicAlbumWithSongsID3 Album { get; set; }

        [JsonPropertyName("song"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicChild Song { get; set; }

        [JsonPropertyName("videos"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicVideos Videos { get; set; }

        [JsonPropertyName("artistInfo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicArtistInfo ArtistInfo { get; set; }

        [JsonPropertyName("artistInfo2"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicArtistInfo ArtistInfo2 { get; set; }

        [JsonPropertyName("lyrics"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicLyrics Lyrics { get; set; }

        [JsonPropertyName("nowPlaying"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicNowPlaying NowPlaying { get; set; }

        [JsonPropertyName("starred"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicStarred Starred { get; set; }

        [JsonPropertyName("starred2"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicStarred2 Starred2 { get; set; }

        [JsonPropertyName("albumList"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicAlbumList AlbumList { get; set; }

        [JsonPropertyName("albumList2"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicAlbumList2 AlbumList2 { get; set; }

        [JsonPropertyName("randomSongs"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicSongs RandomSongs { get; set; }

        [JsonPropertyName("songsByGenre"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicSongs SongsByGenre { get; set; }

        [JsonPropertyName("searchResult2"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicSearchResult2 SearchResult2 { get; set; }

        [JsonPropertyName("searchResult3"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicSearchResult3 SearchResult3 { get; set; }

        [JsonPropertyName("playlists"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicPlaylists Playlists { get; set; }

        [JsonPropertyName("playlist"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicPlaylistWithSongs Playlist { get; set; }

        [JsonPropertyName("user"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicUser User { get; set; }

        [JsonPropertyName("users"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SubsonicUsers Users { get; set; }
    }

    public class SubsonicError {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        // Subsonic error codes used by WaveBox
        public const int Generic = 0;
        public const int MissingParameter = 10;
        public const int ClientTooOld = 20;
        public const int ServerTooOld = 30;
        public const int WrongCredentials = 40;
        public const int TokenAuthNotSupported = 41;
        public const int MechanismNotSupported = 42;
        public const int ConflictingMechanisms = 43;
        public const int InvalidApiKey = 44;
        public const int NotAuthorized = 50;
        public const int NotFound = 70;
    }
}
