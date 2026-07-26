using System;
using System.Diagnostics.CodeAnalysis;
using WaveBox.Core.Model;

namespace WaveBox.Core {
    // The vendored sqlite-net ORM maps types via reflection (GetProperties + Activator.CreateInstance),
    // which the trimmer/NativeAOT can't see.  Rooting every mapped model type here forces full member
    // metadata to be preserved.  Add any new ORM-mapped type to EnsurePreserved or it will fail at
    // runtime under NativeAOT.
    public static class ModelTypeRegistry {
        private static void Root<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() {
        }

        public static void EnsurePreserved() {
            Root<Album>();
            Root<AlbumArtist>();
            Root<AlbumFolder>();
            Root<Art>();
            Root<ArtItem>();
            Root<Artist>();
            Root<Favorite>();
            Root<Folder>();
            Root<Genre>();
            Root<GroupCount>();
            Root<MediaItem>();
            Root<Playlist>();
            Root<PlaylistItem>();
            Root<QueryLog>();
            Root<Session>();
            Root<Song>();
            Root<Stat>();
            Root<User>();
            Root<Video>();
        }
    }
}
