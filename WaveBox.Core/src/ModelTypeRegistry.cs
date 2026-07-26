using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WaveBox.Core.Model;

namespace WaveBox.Core {
    // The vendored sqlite-net ORM maps types via reflection (GetProperties + Activator.CreateInstance),
    // which the trimmer/NativeAOT can't see.  Rooting every mapped model type here forces full member
    // metadata to be preserved.  Add any new ORM-mapped type to EnsurePreserved or it will fail at
    // runtime under NativeAOT.
    public static class ModelTypeRegistry {
        // Each Root<T> call records typeof(T) so tests can verify registry coverage; the Root
        // calls in EnsurePreserved remain the single source of truth.  AOT-safe: no codegen.
        private static readonly List<Type> rootedTypes = new List<Type>();

        // Test accessor (InternalsVisibleTo WaveBox.Core.Tests)
        internal static IReadOnlyList<Type> RootedTypes {
            get {
                EnsurePreserved();
                return rootedTypes;
            }
        }

        private static void Root<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() {
            rootedTypes.Add(typeof(T));
        }

        public static void EnsurePreserved() {
            lock (rootedTypes) {
                if (rootedTypes.Count > 0) {
                    return;
                }
                RootAll();
            }
        }

        private static void RootAll() {
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
