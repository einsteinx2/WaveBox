using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Subsonic;
using Xunit;

namespace WaveBox.Server.Tests {
    public class SubsonicMapperTests {
        [Fact]
        public void Iso8601FormatsKnownValues() {
            Assert.Equal("1970-01-01T00:00:00Z", SubsonicMapper.Iso8601(0));
            Assert.Equal("2017-07-14T02:40:00Z", SubsonicMapper.Iso8601(1500000000));
        }

        [Fact]
        public void Iso8601ReturnsNullForNull() {
            Assert.Null(SubsonicMapper.Iso8601(null));
        }

        [Fact]
        public void GroupByIndexBucketsByFirstLetter() {
            List<string> names = new List<string> { "apple", "Avocado", "banana", "zebra" };

            List<KeyValuePair<string, List<string>>> groups = SubsonicMapper.GroupByIndex(names, s => s);

            Assert.Equal(new[] { "A", "B", "Z" }, groups.Select(g => g.Key).ToArray());
            Assert.Equal(new List<string> { "apple", "Avocado" }, groups[0].Value);
            Assert.Equal(new List<string> { "banana" }, groups[1].Value);
        }

        [Fact]
        public void GroupByIndexPutsNonLettersInHashBucket() {
            List<string> names = new List<string> { "1999", "Éclair", "", null, "Middle" };

            List<KeyValuePair<string, List<string>>> groups = SubsonicMapper.GroupByIndex(names, s => s);

            // '#' sorts before letters ordinally, so it comes first
            Assert.Equal(new[] { "#", "M" }, groups.Select(g => g.Key).ToArray());
            Assert.Equal(new List<string> { "1999", "Éclair", "", null }, groups[0].Value);
        }

        [Fact]
        public void ChildFromSongMapsAllScalarFields() {
            Song song = new Song {
                ItemId = 42,
                FolderId = 7,
                SongName = "Thunderstruck",
                AlbumName = "The Razors Edge",
                ArtistName = "AC/DC",
                AlbumArtistName = "AC/DC (albumartist)",
                TrackNumber = 5,
                DiscNumber = 1,
                ReleaseYear = 1990,
                GenreName = "Rock",
                ArtId = 9,
                FileSize = 12345678,
                FileType = FileType.MP3,
                FileName = "05 Thunderstruck.mp3",
                Duration = 292,
                Bitrate = 320,
                LastModified = 1500000000,
                AlbumId = 11,
                AlbumArtistId = 13
            };

            SubsonicChild child = SubsonicMapper.ChildFromSong(song);

            Assert.Equal("42", child.Id);
            Assert.Equal("7", child.Parent);
            Assert.False(child.IsDir);
            Assert.Equal("Thunderstruck", child.Title);
            Assert.Equal("The Razors Edge", child.Album);
            Assert.Equal("AC/DC", child.Artist);
            Assert.Equal(5, child.Track);
            Assert.Equal(1, child.DiscNumber);
            Assert.Equal(1990, child.Year);
            Assert.Equal("Rock", child.Genre);
            Assert.Equal("9", child.CoverArt);
            Assert.Equal(12345678, child.Size);
            Assert.Equal("audio/mpeg", child.ContentType);
            Assert.Equal("mp3", child.Suffix);
            Assert.Equal(292, child.Duration);
            Assert.Equal(320, child.BitRate);
            Assert.False(child.IsVideo);
            Assert.Equal("2017-07-14T02:40:00Z", child.Created);
            Assert.Equal("11", child.AlbumId);
            Assert.Equal("13", child.ArtistId);
            Assert.Equal("music", child.Type);
        }

        [Fact]
        public void ChildFromSongVirtualPathSanitizesSlashes() {
            Song song = new Song {
                ArtistName = "AC/DC",
                AlbumName = "Back in Black",
                FileName = "01 Hells Bells.mp3"
            };

            SubsonicChild child = SubsonicMapper.ChildFromSong(song);

            Assert.Equal("AC_DC/Back in Black/01 Hells Bells.mp3", child.Path);
        }

        [Fact]
        public void ChildFromSongFallbacksAndNulls() {
            Song song = new Song {
                FileName = "track.flac",
                AlbumArtistName = "Album Artist",
                FileType = FileType.FLAC
            };

            SubsonicChild child = SubsonicMapper.ChildFromSong(song);

            // Title falls back to file name, Artist falls back to album artist
            Assert.Equal("track.flac", child.Title);
            Assert.Equal("Album Artist", child.Artist);
            Assert.Null(child.Id);
            Assert.Null(child.Parent);
            Assert.Null(child.CoverArt);
            Assert.Null(child.Created);
            // No artist/album segments beyond the album artist -> two-part virtual path
            Assert.Equal("Album Artist/track.flac", child.Path);
            Assert.Equal("flac", child.Suffix);
        }

        [Fact]
        public void ChildFromSongWithoutFileNameHasNullPathAndSuffix() {
            SubsonicChild child = SubsonicMapper.ChildFromSong(new Song { SongName = "Untitled" });

            Assert.Null(child.Path);
            Assert.Null(child.Suffix);
            Assert.Equal("Untitled", child.Title);
        }

        [Fact]
        public void ChildFromSongSuffixIsLowercasedExtension() {
            SubsonicChild child = SubsonicMapper.ChildFromSong(new Song { FileName = "SONG.Mp3" });
            Assert.Equal("mp3", child.Suffix);

            // Extensionless file names yield a null suffix
            Assert.Null(SubsonicMapper.ChildFromSong(new Song { FileName = "README" }).Suffix);
        }

        [Fact]
        public void ChildFromVideoMapsVideoFields() {
            Video video = new Video {
                ItemId = 50,
                FolderId = 3,
                FileName = "movie.m4v",
                GenreName = "Documentary",
                ArtId = 4,
                FileSize = 999,
                FileType = FileType.MP4,
                Duration = 3600,
                Bitrate = 2500,
                LastModified = 0
            };

            SubsonicChild child = SubsonicMapper.ChildFromVideo(video);

            Assert.Equal("50", child.Id);
            Assert.Equal("3", child.Parent);
            Assert.False(child.IsDir);
            Assert.Equal("movie", child.Title);
            Assert.Equal("video/mp4", child.ContentType);
            Assert.Equal("m4v", child.Suffix);
            Assert.True(child.IsVideo);
            // Videos expose the raw file name as their path
            Assert.Equal("movie.m4v", child.Path);
            Assert.Equal("1970-01-01T00:00:00Z", child.Created);
            Assert.Equal("video", child.Type);
        }

        [Fact]
        public void ChildFromMediaItemDispatchesOnRuntimeType() {
            Assert.Equal("music", SubsonicMapper.ChildFromMediaItem(new Song { FileName = "a.mp3" }).Type);
            Assert.Equal("video", SubsonicMapper.ChildFromMediaItem(new Video { FileName = "a.m4v" }).Type);
            Assert.Null(SubsonicMapper.ChildFromMediaItem(new MediaItem()));
        }

        [Fact]
        public void ChildFromAlbumMapsFolderFlavoredAlbum() {
            Album album = new Album {
                AlbumId = 20,
                AlbumName = "Powerage",
                AlbumArtistId = 8,
                AlbumArtistName = "AC/DC",
                ReleaseYear = 1978,
                ArtId = 2
            };

            SubsonicChild child = SubsonicMapper.ChildFromAlbum(album);

            Assert.Equal("20", child.Id);
            Assert.Equal("8", child.Parent);
            Assert.True(child.IsDir);
            Assert.Equal("Powerage", child.Title);
            Assert.Equal("Powerage", child.Album);
            Assert.Equal("AC/DC", child.Artist);
            Assert.Equal(1978, child.Year);
            Assert.Equal("2", child.CoverArt);
            Assert.Equal("20", child.AlbumId);
            Assert.Equal("8", child.ArtistId);
        }

        [Fact]
        public void ChildFromAlbumInFolderSwapsIdForFolderId() {
            Album album = new Album { AlbumId = 20, AlbumArtistId = 8, AlbumName = "Powerage" };
            IDictionary<int, int> lookup = new Dictionary<int, int> { { 20, 77 } };

            SubsonicChild child = SubsonicMapper.ChildFromAlbumInFolder(album, lookup);

            Assert.Equal("77", child.Id);
            Assert.Null(child.Parent);
            // Tag-level ids are still reported for display metadata
            Assert.Equal("20", child.AlbumId);
        }

        [Fact]
        public void ChildFromAlbumInFolderWithoutMatchKeepsAlbumId() {
            Album album = new Album { AlbumId = 20, AlbumArtistId = 8 };

            SubsonicChild noLookup = SubsonicMapper.ChildFromAlbumInFolder(album, null);
            Assert.Equal("20", noLookup.Id);
            Assert.Equal("8", noLookup.Parent);

            SubsonicChild noMatch = SubsonicMapper.ChildFromAlbumInFolder(album, new Dictionary<int, int> { { 99, 1 } });
            Assert.Equal("20", noMatch.Id);
            Assert.Equal("8", noMatch.Parent);
        }

        [Fact]
        public void ToFolderLookupSkipsNullIds() {
            IList<AlbumFolder> rows = new List<AlbumFolder> {
                new AlbumFolder { AlbumId = 1, FolderId = 10 },
                new AlbumFolder { AlbumId = null, FolderId = 11 },
                new AlbumFolder { AlbumId = 2, FolderId = null },
                new AlbumFolder { AlbumId = 3, FolderId = 30 }
            };

            IDictionary<int, int> lookup = SubsonicMapper.ToFolderLookup(rows);

            Assert.Equal(2, lookup.Count);
            Assert.Equal(10, lookup[1]);
            Assert.Equal(30, lookup[3]);
        }

        [Fact]
        public void ToLookupKeysByGroupIdAndSkipsNulls() {
            IList<GroupCount> counts = new List<GroupCount> {
                new GroupCount { GroupId = 5, Count = 12, Total = 3600 },
                new GroupCount { GroupId = null, Count = 1, Total = 1 }
            };

            IDictionary<int, GroupCount> lookup = SubsonicMapper.ToLookup(counts);

            Assert.Single(lookup);
            Assert.Equal(12, lookup[5].Count);
            Assert.Equal(3600, lookup[5].Total);
        }

        [Fact]
        public void AlbumID3FromAlbumMapsFieldsAndCounts() {
            Album album = new Album {
                AlbumId = 20,
                AlbumName = "Powerage",
                AlbumArtistId = 8,
                AlbumArtistName = "AC/DC",
                ReleaseYear = 1978,
                ArtId = 2
            };
            IDictionary<int, GroupCount> counts = new Dictionary<int, GroupCount> {
                { 20, new GroupCount { GroupId = 20, Count = 9, Total = 2400 } }
            };

            SubsonicAlbumID3 dto = SubsonicMapper.AlbumID3FromAlbum(album, counts);

            Assert.Equal("20", dto.Id);
            Assert.Equal("Powerage", dto.Name);
            Assert.Equal("AC/DC", dto.Artist);
            Assert.Equal("8", dto.ArtistId);
            Assert.Equal("2", dto.CoverArt);
            Assert.Equal(1978, dto.Year);
            Assert.Equal(9, dto.SongCount);
            Assert.Equal(2400, dto.Duration);
        }

        [Fact]
        public void AlbumID3FromAlbumWithoutCountsLeavesZeroes() {
            SubsonicAlbumID3 dto = SubsonicMapper.AlbumID3FromAlbum(new Album { AlbumId = 20 }, null);

            Assert.Equal(0, dto.SongCount);
            Assert.Equal(0, dto.Duration);
        }

        [Fact]
        public void PlaylistFromPlaylistMapsFieldsAndSynthesizesOwner() {
            Playlist playlist = new Playlist {
                PlaylistId = 4,
                PlaylistName = "Roadtrip",
                PlaylistCount = 25,
                PlaylistDuration = 5400,
                LastUpdateTime = 1500000000
            };

            SubsonicPlaylist dto = SubsonicMapper.PlaylistFromPlaylist(playlist, "ben");

            Assert.Equal("4", dto.Id);
            Assert.Equal("Roadtrip", dto.Name);
            Assert.Equal("ben", dto.Owner);
            Assert.True(dto.Public);
            Assert.Equal(25, dto.SongCount);
            Assert.Equal(5400, dto.Duration);
            Assert.Equal("2017-07-14T02:40:00Z", dto.Created);
            Assert.Equal(dto.Created, dto.Changed);
        }

        [Fact]
        public void PlaylistFromPlaylistNullCountsBecomeZero() {
            SubsonicPlaylist dto = SubsonicMapper.PlaylistFromPlaylist(new Playlist { PlaylistId = 1 }, null);

            Assert.Equal(0, dto.SongCount);
            Assert.Equal(0, dto.Duration);
            Assert.Null(dto.Created);
        }

        [Fact]
        public void UserFromUserDerivesRoleFlags() {
            User user = new User { UserName = "ben", Role = Role.User, LastfmSession = "sess" };
            IList<int> folders = new List<int> { 0 };

            SubsonicUser dto = SubsonicMapper.UserFromUser(user, folders);

            Assert.Equal("ben", dto.Username);
            Assert.True(dto.ScrobblingEnabled);
            Assert.False(dto.AdminRole);
            Assert.True(dto.SettingsRole);
            Assert.True(dto.DownloadRole);
            Assert.True(dto.PlaylistRole);
            Assert.True(dto.CoverArtRole);
            Assert.True(dto.StreamRole);
            Assert.False(dto.UploadRole);
            Assert.False(dto.CommentRole);
            Assert.False(dto.PodcastRole);
            Assert.False(dto.JukeboxRole);
            Assert.False(dto.ShareRole);
            Assert.False(dto.VideoConversionRole);
            Assert.Same(folders, dto.Folder);
        }

        [Fact]
        public void UserFromUserAdminAndGuestFlags() {
            SubsonicUser admin = SubsonicMapper.UserFromUser(new User { UserName = "root", Role = Role.Admin }, new List<int>());
            Assert.True(admin.AdminRole);
            Assert.True(admin.SettingsRole);
            Assert.False(admin.ScrobblingEnabled);

            SubsonicUser guest = SubsonicMapper.UserFromUser(new User { UserName = "guest", Role = Role.Guest }, new List<int>());
            Assert.False(guest.AdminRole);
            Assert.False(guest.SettingsRole);
            Assert.False(guest.PlaylistRole);
            // Streaming is always allowed
            Assert.True(guest.StreamRole);
        }

        [Fact]
        public void FillArtistID3WithoutCoverArtAvoidsArtLookup() {
            // AlbumArtist.ArtId performs a repository lookup, so includeCoverArt: false must
            // never touch it (this test would throw on Injection.Get otherwise)
            AlbumArtist artist = new AlbumArtist { AlbumArtistId = 8, AlbumArtistName = "AC/DC" };

            SubsonicArtistID3 dto = SubsonicMapper.ArtistID3FromAlbumArtist(artist, 5, false);

            Assert.Equal("8", dto.Id);
            Assert.Equal("AC/DC", dto.Name);
            Assert.Equal(5, dto.AlbumCount);
            Assert.Null(dto.CoverArt);
        }
    }
}
