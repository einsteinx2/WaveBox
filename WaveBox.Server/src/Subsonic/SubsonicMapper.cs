using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;

namespace WaveBox.Subsonic {
    // Translates WaveBox domain models into Subsonic DTOs.  All entity ids serialize as
    // strings (the Subsonic schema requires string ids; some clients break on bare ints).
    public static class SubsonicMapper {
        // Unix seconds -> ISO 8601 UTC, the Subsonic date format
        public static string Iso8601(long? unixSeconds) {
            if (unixSeconds == null) {
                return null;
            }
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        public static SubsonicChild ChildFromSong(Song song) {
            return new SubsonicChild {
                Id = song.ItemId == null ? null : song.ItemId.ToString(),
                Parent = song.FolderId == null ? null : song.FolderId.ToString(),
                IsDir = false,
                Title = song.SongName ?? song.FileName,
                Album = song.AlbumName,
                Artist = song.ArtistName ?? song.AlbumArtistName,
                Track = song.TrackNumber,
                DiscNumber = song.DiscNumber,
                Year = song.ReleaseYear,
                Genre = song.GenreName,
                CoverArt = song.ArtId == null ? null : song.ArtId.ToString(),
                Size = song.FileSize,
                ContentType = song.FileType.MimeType(),
                Suffix = Suffix(song.FileName),
                Duration = song.Duration,
                BitRate = song.Bitrate,
                Path = VirtualPath(song.ArtistName ?? song.AlbumArtistName, song.AlbumName, song.FileName),
                IsVideo = false,
                Created = Iso8601(song.LastModified),
                AlbumId = song.AlbumId == null ? null : song.AlbumId.ToString(),
                ArtistId = song.AlbumArtistId == null ? null : song.AlbumArtistId.ToString(),
                Type = "music"
            };
        }

        public static SubsonicChild ChildFromVideo(Video video) {
            return new SubsonicChild {
                Id = video.ItemId == null ? null : video.ItemId.ToString(),
                Parent = video.FolderId == null ? null : video.FolderId.ToString(),
                IsDir = false,
                Title = video.FileName == null ? null : System.IO.Path.GetFileNameWithoutExtension(video.FileName),
                Genre = video.GenreName,
                CoverArt = video.ArtId == null ? null : video.ArtId.ToString(),
                Size = video.FileSize,
                ContentType = video.FileType.MimeType(),
                Suffix = Suffix(video.FileName),
                Duration = video.Duration,
                BitRate = video.Bitrate,
                Path = video.FileName,
                IsVideo = true,
                Created = Iso8601(video.LastModified),
                Type = "video"
            };
        }

        public static SubsonicChild ChildFromMediaItem(IMediaItem item) {
            Song song = item as Song;
            if (song != null) {
                return ChildFromSong(song);
            }
            Video video = item as Video;
            if (video != null) {
                return ChildFromVideo(video);
            }
            return null;
        }

        // Note: reading Folder.ArtId costs one small DB lookup per folder
        public static SubsonicChild ChildFromFolder(Folder folder) {
            int? artId = folder.ArtId;
            return new SubsonicChild {
                Id = folder.FolderId == null ? null : folder.FolderId.ToString(),
                Parent = folder.ParentFolderId == null ? null : folder.ParentFolderId.ToString(),
                IsDir = true,
                Title = folder.FolderName,
                CoverArt = artId == null ? null : artId.ToString()
            };
        }

        // Folder-flavored album entry for getAlbumList and search2
        public static SubsonicChild ChildFromAlbum(Album album) {
            return new SubsonicChild {
                Id = album.AlbumId == null ? null : album.AlbumId.ToString(),
                Parent = album.AlbumArtistId == null ? null : album.AlbumArtistId.ToString(),
                IsDir = true,
                Title = album.AlbumName,
                Album = album.AlbumName,
                Artist = album.AlbumArtistName,
                Year = album.ReleaseYear,
                CoverArt = album.ArtId == null ? null : album.ArtId.ToString(),
                AlbumId = album.AlbumId == null ? null : album.AlbumId.ToString(),
                ArtistId = album.AlbumArtistId == null ? null : album.AlbumArtistId.ToString()
            };
        }

        // Folder-flavored album entry for the non-ID3 endpoints (getAlbumList, getStarred):
        // album metadata for display, but the browsable id is the folder holding the album's
        // songs so folder-mode clients traverse the real directory tree
        public static SubsonicChild ChildFromAlbumInFolder(Album album, IDictionary<int, int> folderByAlbum) {
            SubsonicChild child = ChildFromAlbum(album);

            int folderId;
            if (folderByAlbum != null && album.AlbumId != null && folderByAlbum.TryGetValue((int)album.AlbumId, out folderId)) {
                child.Id = folderId.ToString();
                // The tag artist id would be wrong as a directory parent; leave it unset
                child.Parent = null;
            }

            return child;
        }

        public static IDictionary<int, int> ToFolderLookup(IList<AlbumFolder> albumFolders) {
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            foreach (AlbumFolder albumFolder in albumFolders) {
                if (albumFolder.AlbumId != null && albumFolder.FolderId != null) {
                    lookup[(int)albumFolder.AlbumId] = (int)albumFolder.FolderId;
                }
            }
            return lookup;
        }

        // counts: optional SongCountsByAlbum() lookup keyed by AlbumId
        public static SubsonicAlbumID3 AlbumID3FromAlbum(Album album, IDictionary<int, GroupCount> counts) {
            SubsonicAlbumID3 dto = new SubsonicAlbumID3();
            FillAlbumID3(dto, album, counts);
            return dto;
        }

        public static void FillAlbumID3(SubsonicAlbumID3 dto, Album album, IDictionary<int, GroupCount> counts) {
            dto.Id = album.AlbumId == null ? null : album.AlbumId.ToString();
            dto.Name = album.AlbumName;
            dto.Artist = album.AlbumArtistName;
            dto.ArtistId = album.AlbumArtistId == null ? null : album.AlbumArtistId.ToString();
            dto.CoverArt = album.ArtId == null ? null : album.ArtId.ToString();
            dto.Year = album.ReleaseYear;

            GroupCount stats;
            if (counts != null && album.AlbumId != null && counts.TryGetValue((int)album.AlbumId, out stats)) {
                dto.SongCount = stats.Count;
                dto.Duration = (int)stats.Total;
            }
        }

        // Note: reading AlbumArtist.ArtId costs one small DB lookup; pass includeCoverArt: false
        // when mapping long lists
        public static SubsonicArtistID3 ArtistID3FromAlbumArtist(AlbumArtist albumArtist, int? albumCount, bool includeCoverArt) {
            SubsonicArtistID3 dto = new SubsonicArtistID3();
            FillArtistID3(dto, albumArtist, albumCount, includeCoverArt);
            return dto;
        }

        public static void FillArtistID3(SubsonicArtistID3 dto, AlbumArtist albumArtist, int? albumCount, bool includeCoverArt) {
            dto.Id = albumArtist.AlbumArtistId == null ? null : albumArtist.AlbumArtistId.ToString();
            dto.Name = albumArtist.AlbumArtistName;
            dto.AlbumCount = albumCount;
            if (includeCoverArt) {
                int? artId = albumArtist.ArtId;
                dto.CoverArt = artId == null ? null : artId.ToString();
            }
        }

        public static SubsonicPlaylist PlaylistFromPlaylist(Playlist playlist, string owner) {
            SubsonicPlaylist dto = new SubsonicPlaylist();
            FillPlaylist(dto, playlist, owner);
            return dto;
        }

        public static void FillPlaylist(SubsonicPlaylist dto, Playlist playlist, string owner) {
            dto.Id = playlist.PlaylistId == null ? null : playlist.PlaylistId.ToString();
            dto.Name = playlist.PlaylistName;
            dto.Owner = owner;
            dto.Public = true;
            dto.SongCount = playlist.PlaylistCount ?? 0;
            dto.Duration = playlist.PlaylistDuration ?? 0;
            dto.Created = Iso8601(playlist.LastUpdateTime);
            dto.Changed = Iso8601(playlist.LastUpdateTime);
        }

        public static SubsonicUser UserFromUser(User user, IList<int> folderIds) {
            bool userRole = user.HasPermission(Role.User);
            return new SubsonicUser {
                Username = user.UserName,
                ScrobblingEnabled = user.LastfmSession != null,
                AdminRole = user.HasPermission(Role.Admin),
                SettingsRole = userRole,
                DownloadRole = userRole,
                UploadRole = false,
                PlaylistRole = userRole,
                CoverArtRole = userRole,
                CommentRole = false,
                PodcastRole = false,
                StreamRole = true,
                JukeboxRole = false,
                ShareRole = false,
                VideoConversionRole = false,
                Folder = folderIds
            };
        }

        // Group a sorted list into Subsonic index buckets: A-Z by first letter, everything else
        // under "#".  Items must already be sorted by name (the repositories sort NOCASE).
        public static List<KeyValuePair<string, List<T>>> GroupByIndex<T>(IEnumerable<T> items, Func<T, string> nameOf) {
            Dictionary<string, List<T>> buckets = new Dictionary<string, List<T>>(StringComparer.Ordinal);
            List<string> order = new List<string>();

            foreach (T item in items) {
                string name = nameOf(item);
                string key = "#";
                if (!String.IsNullOrEmpty(name)) {
                    char first = Char.ToUpperInvariant(name[0]);
                    if (first >= 'A' && first <= 'Z') {
                        key = first.ToString();
                    }
                }

                List<T> bucket;
                if (!buckets.TryGetValue(key, out bucket)) {
                    bucket = new List<T>();
                    buckets[key] = bucket;
                    order.Add(key);
                }
                bucket.Add(item);
            }

            order.Sort(StringComparer.Ordinal);
            return order.Select(key => new KeyValuePair<string, List<T>>(key, buckets[key])).ToList();
        }

        public static IDictionary<int, GroupCount> ToLookup(IList<GroupCount> counts) {
            Dictionary<int, GroupCount> lookup = new Dictionary<int, GroupCount>();
            foreach (GroupCount count in counts) {
                if (count.GroupId != null) {
                    lookup[(int)count.GroupId] = count;
                }
            }
            return lookup;
        }

        private static string Suffix(string fileName) {
            if (String.IsNullOrEmpty(fileName)) {
                return null;
            }
            string extension = System.IO.Path.GetExtension(fileName);
            return String.IsNullOrEmpty(extension) ? null : extension.TrimStart('.').ToLowerInvariant();
        }

        // Synthetic Artist/Album/File path: real filesystem paths must not leak to clients
        private static string VirtualPath(string artist, string album, string fileName) {
            if (String.IsNullOrEmpty(fileName)) {
                return null;
            }
            string clean(string part) {
                return part == null ? null : part.Replace('/', '_');
            }
            List<string> parts = new List<string>();
            if (!String.IsNullOrEmpty(artist)) {
                parts.Add(clean(artist));
            }
            if (!String.IsNullOrEmpty(album)) {
                parts.Add(clean(album));
            }
            parts.Add(clean(fileName));
            return String.Join("/", parts);
        }
    }
}
