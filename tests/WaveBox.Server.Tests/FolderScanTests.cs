using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.FolderScanning;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class FolderScanTests : IDisposable {
        private readonly IntegrationHarness harness;

        public FolderScanTests() {
            harness = new IntegrationHarness();
        }

        public void Dispose() {
            harness.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void ScanCreatesSongWithFixtureMetadata() {
            harness.SeedMediaFolder();

            IList<Song> songs = Injection.Get<ISongRepository>().AllSongs();
            Song song = Assert.Single(songs);

            Assert.Equal("Test Song", song.SongName);
            Assert.Equal("Test Artist", song.ArtistName);
            Assert.Equal("Test Album", song.AlbumName);
            Assert.Equal("Rock", song.GenreName);
            Assert.Equal(FileType.MP3, song.FileType);
            Assert.True(song.Duration > 0);
            Assert.True(song.Bitrate > 0);
            Assert.True(song.FileSize > 0);
            Assert.NotNull(song.FolderId);
            Assert.Equal("Test Song.mp3", song.FileName);
        }

        [Fact]
        public void ScanCreatesArtistAlbumArtistAlbumAndGenreRows() {
            harness.SeedMediaFolder();

            Assert.NotNull(Injection.Get<IArtistRepository>().ArtistForName("Test Artist").ArtistId);
            // No TPE2 in the fixture, so the album artist falls back to the artist name
            Assert.NotNull(Injection.Get<IAlbumArtistRepository>().AlbumArtistForName("Test Artist").AlbumArtistId);
            Assert.Equal(1, Injection.Get<IAlbumRepository>().CountAlbums());
            Assert.Equal("Test Album", Injection.Get<IAlbumRepository>().AllAlbums()[0].AlbumName);
            Assert.NotNull(Injection.Get<IGenreRepository>().GenreForName("Rock").GenreId);
        }

        [Fact]
        public void ScanRegistersMediaFolderAndFolderTree() {
            string mediaDir = harness.SeedMediaFolder();

            IList<Folder> mediaFolders = Injection.Get<IFolderRepository>().MediaFolders();
            Assert.Contains(mediaFolders, f => f.FolderPath == mediaDir);

            Folder folder = Injection.Get<IFolderRepository>().FolderForPath(mediaDir);
            Assert.NotNull(folder.FolderId);
            Assert.Single(Injection.Get<IFolderRepository>().ListOfSongs((int)folder.FolderId));
        }

        [Fact]
        public void SongNeedsUpdatingFalseAfterScanTrueAfterTouch() {
            string mediaDir = harness.SeedMediaFolder();
            string file = Path.Combine(mediaDir, "Test Song.mp3");
            int? folderId = Injection.Get<IFolderRepository>().FolderForPath(mediaDir).FolderId;
            FolderScanOperation op = new FolderScanOperation(mediaDir, 0);

            bool isNew;
            int? itemId;
            Assert.False(op.SongNeedsUpdating(file, folderId, out isNew, out itemId));
            Assert.False(isNew);
            Assert.NotNull(itemId);

            File.SetLastWriteTime(file, File.GetLastWriteTime(file).AddMinutes(2));

            Assert.True(op.SongNeedsUpdating(file, folderId, out isNew, out itemId));
            Assert.False(isNew);
            Assert.NotNull(itemId);
        }

        [Fact]
        public void RescanDoesNotDuplicateSongs() {
            string mediaDir = harness.SeedMediaFolder();
            Assert.Equal(1, Injection.Get<ISongRepository>().CountSongs());

            new FolderScanOperation(mediaDir, 0).Start();

            Assert.Equal(1, Injection.Get<ISongRepository>().CountSongs());
        }

        [Fact]
        public void RescanAfterTouchUpdatesSongInPlace() {
            string mediaDir = harness.SeedMediaFolder();
            string file = Path.Combine(mediaDir, "Test Song.mp3");
            Song original = Injection.Get<ISongRepository>().AllSongs()[0];

            File.SetLastWriteTime(file, File.GetLastWriteTime(file).AddMinutes(2));
            new FolderScanOperation(mediaDir, 0).Start();

            IList<Song> songs = Injection.Get<ISongRepository>().AllSongs();
            Song updated = Assert.Single(songs);
            Assert.Equal(original.ItemId, updated.ItemId);
            Assert.True(updated.LastModified > original.LastModified);
        }

        [Fact]
        public void ScanPicksUpSubfolders() {
            string mediaDir = harness.SeedMediaFolder("Root Song");
            string subDir = Path.Combine(mediaDir, "Deeper");
            Directory.CreateDirectory(subDir);
            WaveBox.TestFixtures.Mp3Fixture.Write(Path.Combine(subDir, "Nested Song.mp3"), title: "Nested Song");

            new FolderScanOperation(mediaDir, 0).Start();

            IList<Song> songs = Injection.Get<ISongRepository>().AllSongs();
            Assert.Equal(2, songs.Count);
            Assert.Contains(songs, s => s.SongName == "Nested Song");

            Folder sub = Injection.Get<IFolderRepository>().FolderForPath(subDir);
            Assert.NotNull(sub.FolderId);
            Assert.NotNull(sub.ParentFolderId);
        }
    }
}
