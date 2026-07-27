using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class PlaylistTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly List<Song> songs;

        public PlaylistTests() {
            harness = new IntegrationHarness();
            harness.SeedMediaFolder("Alpha", "Bravo", "Charlie");
            songs = Injection.Get<ISongRepository>().AllSongs().OrderBy(s => s.SongName, StringComparer.Ordinal).ToList();
            Assert.Equal(3, songs.Count);
        }

        public void Dispose() {
            harness.Dispose();
        }

        private static Playlist Create(string name) {
            Playlist playlist = new Playlist { PlaylistName = name };
            playlist.CreatePlaylist();
            return playlist;
        }

        private static List<int?> ItemIds(Playlist playlist) {
            return playlist.ListOfMediaItems().Select(i => i.ItemId).ToList();
        }

        [Fact]
        public void CreatePlaylistAssignsIdAndDefaults() {
            Playlist playlist = Create("empty");

            Assert.NotNull(playlist.PlaylistId);
            Assert.Equal(0, playlist.PlaylistCount);
            Assert.Equal(0, playlist.PlaylistDuration);
            Assert.NotNull(playlist.LastUpdateTime);
            Assert.Equal(playlist.CalculateHash(), playlist.Md5Hash);

            Playlist fetched = Injection.Get<IPlaylistRepository>().PlaylistForId((int)playlist.PlaylistId);
            Assert.Equal("empty", fetched.PlaylistName);
            Assert.Equal(0, fetched.PlaylistCount);
        }

        [Fact]
        public void AddMediaItemIncrementsCountDurationAndPersists() {
            Playlist playlist = Create("one");

            playlist.AddMediaItem(songs[0]);

            Assert.Equal(1, playlist.PlaylistCount);
            Assert.Equal(songs[0].Duration, playlist.PlaylistDuration);
            Assert.Equal(new List<int?> { songs[0].ItemId }, ItemIds(playlist));

            Playlist fetched = Injection.Get<IPlaylistRepository>().PlaylistForId((int)playlist.PlaylistId);
            Assert.Equal(1, fetched.PlaylistCount);
            Assert.Equal(playlist.Md5Hash, fetched.Md5Hash);
        }

        [Fact]
        public void DuplicateAddsAreAllowed() {
            Playlist playlist = Create("dupes");

            playlist.AddMediaItem(songs[0]);
            playlist.AddMediaItem(songs[0]);

            Assert.Equal(2, playlist.PlaylistCount);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[0].ItemId }, ItemIds(playlist));
        }

        [Fact]
        public void AddMediaItemsAppendsInOrder() {
            Playlist playlist = Create("ordered");

            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());

            Assert.Equal(3, playlist.PlaylistCount);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[1].ItemId, songs[2].ItemId }, ItemIds(playlist));
            Assert.Equal(songs.Sum(s => s.Duration ?? 0), playlist.PlaylistDuration);
        }

        [Fact]
        public void AddMediaItemsByIdResolvesThroughMediaItemRepository() {
            Playlist playlist = Create("by-id");

            playlist.AddMediaItems(songs.Select(s => (int)s.ItemId).ToList());

            Assert.Equal(3, playlist.PlaylistCount);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[1].ItemId, songs[2].ItemId }, ItemIds(playlist));
        }

        [Fact]
        public void Md5HashChangesWithContentAndMatchesRecompute() {
            Playlist playlist = Create("hash");
            string emptyHash = playlist.Md5Hash;

            playlist.AddMediaItem(songs[0]);
            string oneHash = playlist.Md5Hash;

            Assert.NotEqual(emptyHash, oneHash);
            Assert.Equal(playlist.CalculateHash(), oneHash);

            playlist.AddMediaItem(songs[1]);
            Assert.NotEqual(oneHash, playlist.Md5Hash);
            Assert.Equal(playlist.CalculateHash(), playlist.Md5Hash);
        }

        [Fact]
        public void RemoveMediaItemAtIndexesReindexesAndRecomputesCounts() {
            Playlist playlist = Create("remove");
            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());

            playlist.RemoveMediaItemAtIndexes(new List<int> { 1 });

            Assert.Equal(2, playlist.PlaylistCount);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[2].ItemId }, ItemIds(playlist));
            Assert.Equal((songs[0].Duration ?? 0) + (songs[2].Duration ?? 0), playlist.PlaylistDuration);

            // Positions are contiguous again, so a later add appends cleanly at the end
            playlist.AddMediaItem(songs[1]);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[2].ItemId, songs[1].ItemId }, ItemIds(playlist));
        }

        [Fact]
        public void MoveMediaItemReordersPlaylist() {
            Playlist playlist = Create("move");
            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());

            // [A, B, C]: move index 0 to index 2 -> [B, C, A]
            playlist.MoveMediaItem(0, 2);
            Assert.Equal(new List<int?> { songs[1].ItemId, songs[2].ItemId, songs[0].ItemId }, ItemIds(playlist));

            // Move it back to the front -> [A, B, C]
            playlist.MoveMediaItem(2, 0);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[1].ItemId, songs[2].ItemId }, ItemIds(playlist));
        }

        [Fact]
        public void MoveMediaItemIgnoresOutOfRangeInput() {
            Playlist playlist = Create("move-noop");
            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());
            var before = ItemIds(playlist);

            playlist.MoveMediaItem(5, 0);
            playlist.MoveMediaItem(-1, 1);
            playlist.MoveMediaItem(1, 1);

            Assert.Equal(before, ItemIds(playlist));
        }

        [Fact]
        public void InsertMediaItemPlacesItemAtIndex() {
            Playlist playlist = Create("insert");
            playlist.AddMediaItem(songs[0]);
            playlist.AddMediaItem(songs[2]);

            playlist.InsertMediaItem(songs[1], 1);

            Assert.Equal(3, playlist.PlaylistCount);
            Assert.Equal(new List<int?> { songs[0].ItemId, songs[1].ItemId, songs[2].ItemId }, ItemIds(playlist));
        }

        [Fact]
        public void IndexOfMediaItemReturnsFirstPositionOfItemType() {
            // Pins actual behavior: IndexOfMediaItem filters by ItemType only, ignoring the
            // specific item id - it always returns the lowest song position
            Playlist playlist = Create("index-quirk");
            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());

            Assert.Equal(0, playlist.IndexOfMediaItem(songs[2]));
        }

        [Fact]
        public void ClearPlaylistEmptiesItemsButKeepsPlaylist() {
            Playlist playlist = Create("clear");
            playlist.AddMediaItems(songs.Cast<IMediaItem>().ToList());

            playlist.ClearPlaylist();

            Assert.Equal(0, playlist.PlaylistCount);
            Assert.Equal(0, playlist.PlaylistDuration);
            Assert.Empty(playlist.ListOfMediaItems());
            Assert.Equal("clear", Injection.Get<IPlaylistRepository>().PlaylistForId((int)playlist.PlaylistId).PlaylistName);
        }

        [Fact]
        public void DeletePlaylistRemovesPlaylistRow() {
            Playlist playlist = Create("doomed");
            playlist.AddMediaItem(songs[0]);
            int id = (int)playlist.PlaylistId;

            playlist.DeletePlaylist();

            // GetSingle returns a stub with null id when the row is gone
            Assert.Null(Injection.Get<IPlaylistRepository>().PlaylistForId(id).PlaylistId);
            Assert.Empty(Injection.Get<IPlaylistRepository>().AllPlaylists());
        }
    }
}
