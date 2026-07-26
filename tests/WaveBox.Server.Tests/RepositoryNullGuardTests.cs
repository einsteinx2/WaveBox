using System;
using NSubstitute;
using WaveBox.Core;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    // Constructor null-guard checks only.  Constructing repositories with real (mock) dependencies
    // can run table-loading logic (e.g. UserRepository.ReloadUsers), so these tests stop at the
    // ArgumentNullException thrown before any I/O happens.
    public class RepositoryNullGuardTests {
        private static readonly IDatabase database = Substitute.For<IDatabase>();
        private static readonly IItemRepository itemRepository = Substitute.For<IItemRepository>();
        private static readonly ISongRepository songRepository = Substitute.For<ISongRepository>();

        [Fact]
        public void SingleDependencyRepositoriesRequireDatabase() {
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new ItemRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new SongRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new VideoRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new GenreRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new ArtRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new PlaylistRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new SessionRepository(null)).ParamName);
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new StatRepository(null)).ParamName);
        }

        [Fact]
        public void UserRepositoryNullGuards() {
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new UserRepository(null, itemRepository)).ParamName);
            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new UserRepository(database, null)).ParamName);
        }

        [Fact]
        public void AlbumRepositoryNullGuards() {
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new AlbumRepository(null, itemRepository)).ParamName);
            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new AlbumRepository(database, null)).ParamName);
        }

        [Fact]
        public void ArtistRepositoryNullGuards() {
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new ArtistRepository(null, itemRepository)).ParamName);
            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new ArtistRepository(database, null)).ParamName);
        }

        [Fact]
        public void AlbumArtistRepositoryNullGuards() {
            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new AlbumArtistRepository(null, itemRepository, songRepository)).ParamName);
            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new AlbumArtistRepository(database, null, songRepository)).ParamName);
            Assert.Equal("songRepository", Assert.Throws<ArgumentNullException>(() => new AlbumArtistRepository(database, itemRepository, null)).ParamName);
        }

        [Fact]
        public void FolderRepositoryNullGuards() {
            IServerSettings serverSettings = Substitute.For<IServerSettings>();
            IVideoRepository videoRepository = Substitute.For<IVideoRepository>();

            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new FolderRepository(null, serverSettings, songRepository, videoRepository)).ParamName);
            Assert.Equal("serverSettings", Assert.Throws<ArgumentNullException>(() => new FolderRepository(database, null, songRepository, videoRepository)).ParamName);
            Assert.Equal("songRepository", Assert.Throws<ArgumentNullException>(() => new FolderRepository(database, serverSettings, null, videoRepository)).ParamName);
            Assert.Equal("videoRepository", Assert.Throws<ArgumentNullException>(() => new FolderRepository(database, serverSettings, songRepository, null)).ParamName);
        }

        [Fact]
        public void MediaItemRepositoryNullGuards() {
            IVideoRepository videoRepository = Substitute.For<IVideoRepository>();

            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new MediaItemRepository(null, songRepository, videoRepository)).ParamName);
            Assert.Equal("songRepository", Assert.Throws<ArgumentNullException>(() => new MediaItemRepository(itemRepository, null, videoRepository)).ParamName);
            Assert.Equal("videoRepository", Assert.Throws<ArgumentNullException>(() => new MediaItemRepository(itemRepository, songRepository, null)).ParamName);
        }

        [Fact]
        public void FavoriteRepositoryNullGuards() {
            IAlbumArtistRepository albumArtistRepository = Substitute.For<IAlbumArtistRepository>();
            IAlbumRepository albumRepository = Substitute.For<IAlbumRepository>();
            IArtistRepository artistRepository = Substitute.For<IArtistRepository>();
            IFolderRepository folderRepository = Substitute.For<IFolderRepository>();
            IGenreRepository genreRepository = Substitute.For<IGenreRepository>();
            IPlaylistRepository playlistRepository = Substitute.For<IPlaylistRepository>();
            IVideoRepository videoRepository = Substitute.For<IVideoRepository>();

            Assert.Equal("database", Assert.Throws<ArgumentNullException>(() => new FavoriteRepository(
                null, albumArtistRepository, albumRepository, artistRepository, folderRepository,
                genreRepository, playlistRepository, songRepository, videoRepository, itemRepository)).ParamName);
            Assert.Equal("albumRepository", Assert.Throws<ArgumentNullException>(() => new FavoriteRepository(
                database, albumArtistRepository, null, artistRepository, folderRepository,
                genreRepository, playlistRepository, songRepository, videoRepository, itemRepository)).ParamName);
            Assert.Equal("itemRepository", Assert.Throws<ArgumentNullException>(() => new FavoriteRepository(
                database, albumArtistRepository, albumRepository, artistRepository, folderRepository,
                genreRepository, playlistRepository, songRepository, videoRepository, null)).ParamName);
        }
    }
}
