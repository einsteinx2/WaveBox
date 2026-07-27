using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    // Smoke-level coverage for the remaining repositories, backed by a real scan of three
    // fixture MP3s (same artist/album/genre, titles Alpha/Bravo/Charlie).
    [Collection("Integration")]
    public class RepositorySmokeTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly List<Song> songs;

        public RepositorySmokeTests() {
            harness = new IntegrationHarness();
            harness.SeedMediaFolder("Alpha", "Bravo", "Charlie");
            songs = Injection.Get<ISongRepository>().AllSongs().OrderBy(s => s.SongName, StringComparer.Ordinal).ToList();
        }

        public void Dispose() {
            harness.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void SongRepositoryQueriesRoundTrip() {
            ISongRepository repo = Injection.Get<ISongRepository>();

            Assert.Equal(3, repo.CountSongs());
            Assert.Equal(songs[0].SongName, repo.SongForId((int)songs[0].ItemId).SongName);
            Assert.Single(repo.SearchSongs("SongName", "Alpha", false));
            Assert.Equal(2, repo.SongsForIds(new List<int> { (int)songs[0].ItemId, (int)songs[1].ItemId }).Count);
            Assert.True(repo.TotalSongDuration() > 0);
            Assert.True(repo.TotalSongSize() > 0);
        }

        [Fact]
        public void AlbumRepositoryAggregatesScannedSongs() {
            IAlbumRepository repo = Injection.Get<IAlbumRepository>();

            Assert.Equal(1, repo.CountAlbums());
            Album album = repo.AllAlbums()[0];
            Assert.Equal("Test Album", album.AlbumName);
            Assert.Equal(album.AlbumName, repo.AlbumForId((int)album.AlbumId).AlbumName);

            Dictionary<int, GroupCount> counts = repo.SongCountsByAlbum().ToDictionary(c => (int)c.GroupId);
            Assert.Equal(3, counts[(int)album.AlbumId].Count);
        }

        [Fact]
        public void ArtistAndAlbumArtistRepositoriesRoundTrip() {
            IArtistRepository artists = Injection.Get<IArtistRepository>();

            Assert.Equal(1, artists.CountArtists());
            Artist artist = artists.ArtistForName("Test Artist");
            Assert.NotNull(artist.ArtistId);

            // ArtistForNameOrCreate creates missing artists on demand
            Artist created = artists.ArtistForNameOrCreate("Brand New Artist");
            Assert.NotNull(created.ArtistId);
            Assert.Equal(2, artists.CountArtists());

            IAlbumArtistRepository albumArtists = Injection.Get<IAlbumArtistRepository>();
            Assert.NotNull(albumArtists.AlbumArtistForName("Test Artist").AlbumArtistId);
        }

        [Fact]
        public void GenreRepositoryTracksScannedGenre() {
            IGenreRepository repo = Injection.Get<IGenreRepository>();

            Genre rock = repo.GenreForName("Rock");
            Assert.NotNull(rock.GenreId);
            Assert.Single(repo.AllGenres());
            Assert.Equal(3, repo.ListOfSongs((int)rock.GenreId).Count);

            IList<GroupCount> counts = repo.SongCountsByGenre();
            Assert.Single(counts);
            Assert.Equal(3, counts[0].Count);
        }

        [Fact]
        public void FavoriteRepositoryAddQueryDeleteRoundTrip() {
            User user = Injection.Get<IUserRepository>().CreateUser("fav", "pw", Role.User, null);
            IFavoriteRepository repo = Injection.Get<IFavoriteRepository>();

            int? favoriteId = repo.AddFavorite((int)user.UserId, (int)songs[0].ItemId, ItemType.Song);
            Assert.NotNull(favoriteId);

            IList<Favorite> favorites = repo.FavoritesForUserId((int)user.UserId);
            Assert.Single(favorites);
            Assert.Equal(songs[0].ItemId, favorites[0].FavoriteItemId);

            IList<IItem> items = repo.ItemsForUserId((int)user.UserId);
            Assert.Single(items);
            Assert.Equal(songs[0].ItemId, items[0].ItemId);

            Assert.True(repo.DeleteFavorite((int)favoriteId));
            Assert.Empty(repo.FavoritesForUserId((int)user.UserId));
        }

        [Fact]
        public void StatRepositoryRecordsPlaybackStats() {
            IStatRepository repo = Injection.Get<IStatRepository>();

            Assert.True(repo.RecordStat((int)songs[0].ItemId, StatType.PLAYED, DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond));
        }

        [Fact]
        public void ItemRepositoryKnowsGeneratedItemTypes() {
            IItemRepository repo = Injection.Get<IItemRepository>();

            Assert.Equal(ItemType.Song, repo.ItemTypeForItemId((int)songs[0].ItemId));

            int? generated = repo.GenerateItemId(ItemType.Playlist);
            Assert.NotNull(generated);
            Assert.Equal(ItemType.Playlist, repo.ItemTypeForItemId((int)generated));
        }

        [Fact]
        public void MediaItemRepositoryResolvesSongsPolymorphically() {
            IMediaItem item = Injection.Get<IMediaItemRepository>().MediaItemForId((int)songs[1].ItemId);

            Song song = Assert.IsType<Song>(item);
            Assert.Equal("Bravo", song.SongName);
        }

        [Fact]
        public void PlaylistRepositoryListsCreatedPlaylists() {
            Playlist playlist = new Playlist { PlaylistName = "smoke" };
            playlist.CreatePlaylist();

            IPlaylistRepository repo = Injection.Get<IPlaylistRepository>();
            Assert.Single(repo.AllPlaylists());
            Assert.Equal("smoke", repo.PlaylistForName("smoke").PlaylistName);
            Assert.Equal(playlist.PlaylistId, repo.PlaylistForId((int)playlist.PlaylistId).PlaylistId);
        }
    }
}
