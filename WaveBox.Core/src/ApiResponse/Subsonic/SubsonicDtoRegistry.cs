using System;
using System.Diagnostics.CodeAnalysis;

namespace WaveBox.Core.ApiResponse.Subsonic {
    // The Subsonic XML serializer walks DTO properties via reflection, which the trimmer/NativeAOT
    // can't see (same situation as the sqlite-net ORM and ModelTypeRegistry).  Rooting every
    // Subsonic DTO type here preserves full member metadata.  Add any new Subsonic DTO to
    // EnsurePreserved or its XML rendering will silently lose properties under NativeAOT.
    public static class SubsonicDtoRegistry {
        private static void Root<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() {
        }

        public static void EnsurePreserved() {
            Root<SubsonicResponse>();
            Root<SubsonicResponseBody>();
            Root<SubsonicError>();
            Root<SubsonicLicense>();
            Root<SubsonicExtension>();
            Root<SubsonicTokenInfo>();
            Root<SubsonicScanStatus>();
            Root<SubsonicArtistInfo>();
            Root<SubsonicMusicFolders>();
            Root<SubsonicMusicFolder>();
            Root<SubsonicIndexes>();
            Root<SubsonicIndex>();
            Root<SubsonicIndexArtist>();
            Root<SubsonicDirectory>();
            Root<SubsonicChild>();
            Root<SubsonicGenres>();
            Root<SubsonicGenre>();
            Root<SubsonicVideos>();
            Root<SubsonicLyrics>();
            Root<SubsonicArtistsID3>();
            Root<SubsonicIndexID3>();
            Root<SubsonicArtistID3>();
            Root<SubsonicArtistWithAlbumsID3>();
            Root<SubsonicAlbumID3>();
            Root<SubsonicAlbumWithSongsID3>();
            Root<SubsonicAlbumList>();
            Root<SubsonicAlbumList2>();
            Root<SubsonicSongs>();
            Root<SubsonicNowPlaying>();
            Root<SubsonicNowPlayingEntry>();
            Root<SubsonicStarred>();
            Root<SubsonicStarred2>();
            Root<SubsonicSearchResult2>();
            Root<SubsonicSearchResult3>();
            Root<SubsonicPlaylists>();
            Root<SubsonicPlaylist>();
            Root<SubsonicPlaylistWithSongs>();
            Root<SubsonicUsers>();
            Root<SubsonicUser>();
        }
    }
}
