using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicBrowsingHandlers {
        public static void GetMusicFolders(SubsonicRequest req, HttpContextProcessor processor, User user) {
            List<SubsonicMusicFolder> folders = new List<SubsonicMusicFolder>();
            foreach (Folder root in Injection.Get<IFolderRepository>().MediaFolders()) {
                folders.Add(new SubsonicMusicFolder {
                    Id = root.FolderId == null ? null : root.FolderId.ToString(),
                    Name = root.FolderName
                });
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.MusicFolders = new SubsonicMusicFolders { MusicFolder = folders };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetIndexes(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IFolderRepository folderRepository = Injection.Get<IFolderRepository>();
            int? musicFolderId = req.GetInt("musicFolderId");

            // Top-level entries across the requested media folder(s) plus loose media at the roots
            List<Folder> topLevel = new List<Folder>();
            List<SubsonicChild> looseMedia = new List<SubsonicChild>();
            foreach (Folder root in folderRepository.MediaFolders()) {
                if (musicFolderId != null && root.FolderId != musicFolderId) {
                    continue;
                }
                topLevel.AddRange(folderRepository.ListOfSubFolders((int)root.FolderId));
                looseMedia.AddRange(folderRepository.ListOfSongs((int)root.FolderId).Select(SubsonicMapper.ChildFromSong));
                looseMedia.AddRange(folderRepository.ListOfVideos((int)root.FolderId).Select(SubsonicMapper.ChildFromVideo));
            }
            topLevel.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.FolderName, y.FolderName));

            List<SubsonicIndex> indexes = new List<SubsonicIndex>();
            foreach (KeyValuePair<string, List<Folder>> bucket in SubsonicMapper.GroupByIndex(topLevel, f => f.FolderName)) {
                indexes.Add(new SubsonicIndex {
                    Name = bucket.Key,
                    Artist = bucket.Value.Select(f => new SubsonicIndexArtist {
                        Id = f.FolderId == null ? null : f.FolderId.ToString(),
                        Name = f.FolderName
                    }).ToList()
                });
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Indexes = new SubsonicIndexes {
                LastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IgnoredArticles = "",
                Index = indexes,
                Child = looseMedia.Count > 0 ? looseMedia : null
            };
            SubsonicWriter.Write(req, processor, body);
        }

        // Accepts folder ids (the true directory tree) but also album and album-artist ids,
        // because getAlbumList/search2 hand out album entries as browsable directories to
        // folder-mode clients.  The global item id space makes the type resolvable from the id.
        public static void GetMusicDirectory(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            SubsonicDirectory directory = null;
            ItemType itemType = Injection.Get<IItemRepository>().ItemTypeForItemId((int)id);

            if (itemType == ItemType.Folder) {
                IFolderRepository folderRepository = Injection.Get<IFolderRepository>();
                Folder folder = folderRepository.FolderForId((int)id);
                if (folder != null && folder.FolderId != null) {
                    List<SubsonicChild> children = new List<SubsonicChild>();
                    children.AddRange(folderRepository.ListOfSubFolders((int)folder.FolderId).Select(SubsonicMapper.ChildFromFolder));
                    children.AddRange(folderRepository.ListOfSongs((int)folder.FolderId).Select(SubsonicMapper.ChildFromSong));
                    children.AddRange(folderRepository.ListOfVideos((int)folder.FolderId).Select(SubsonicMapper.ChildFromVideo));

                    directory = new SubsonicDirectory {
                        Id = folder.FolderId.ToString(),
                        Parent = folder.ParentFolderId == null ? null : folder.ParentFolderId.ToString(),
                        Name = folder.FolderName,
                        Child = children
                    };
                }
            } else if (itemType == ItemType.Album) {
                Album album = Injection.Get<IAlbumRepository>().AlbumForId((int)id);
                if (album != null && album.AlbumId != null) {
                    directory = new SubsonicDirectory {
                        Id = album.AlbumId.ToString(),
                        Parent = album.AlbumArtistId == null ? null : album.AlbumArtistId.ToString(),
                        Name = album.AlbumName,
                        Child = album.ListOfSongs().Select(SubsonicMapper.ChildFromSong).ToList()
                    };
                }
            } else if (itemType == ItemType.AlbumArtist) {
                AlbumArtist albumArtist = Injection.Get<IAlbumArtistRepository>().AlbumArtistForId(id);
                if (albumArtist != null && albumArtist.AlbumArtistId != null) {
                    directory = new SubsonicDirectory {
                        Id = albumArtist.AlbumArtistId.ToString(),
                        Name = albumArtist.AlbumArtistName,
                        Child = albumArtist.ListOfAlbums().Select(SubsonicMapper.ChildFromAlbum).ToList()
                    };
                }
            }

            if (directory == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Directory not found");
                return;
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Directory = directory;
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetArtists(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<AlbumArtist> albumArtists = Injection.Get<IAlbumArtistRepository>().AllAlbumArtists();
            IDictionary<int, GroupCount> albumCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().AlbumCountsByAlbumArtist());

            List<SubsonicIndexID3> indexes = new List<SubsonicIndexID3>();
            foreach (KeyValuePair<string, List<AlbumArtist>> bucket in SubsonicMapper.GroupByIndex(albumArtists, a => a.AlbumArtistName)) {
                indexes.Add(new SubsonicIndexID3 {
                    Name = bucket.Key,
                    Artist = bucket.Value.Select(a => {
                        GroupCount count;
                        int? albumCount = a.AlbumArtistId != null && albumCounts.TryGetValue((int)a.AlbumArtistId, out count) ? count.Count : (int?)0;
                        // includeCoverArt: false — ArtId is a DB lookup per artist, too costly for the full index
                        return SubsonicMapper.ArtistID3FromAlbumArtist(a, albumCount, false);
                    }).ToList()
                });
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Artists = new SubsonicArtistsID3 { IgnoredArticles = "", Index = indexes };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetArtist(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            AlbumArtist albumArtist = Injection.Get<IAlbumArtistRepository>().AlbumArtistForId(id);
            if (albumArtist == null || albumArtist.AlbumArtistId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Artist not found");
                return;
            }

            IList<Album> albums = albumArtist.ListOfAlbums();
            IDictionary<int, GroupCount> songCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().SongCountsByAlbum());

            SubsonicArtistWithAlbumsID3 dto = new SubsonicArtistWithAlbumsID3();
            SubsonicMapper.FillArtistID3(dto, albumArtist, albums.Count, true);
            dto.Album = albums.Select(a => SubsonicMapper.AlbumID3FromAlbum(a, songCounts)).ToList();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Artist = dto;
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetAlbum(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            Album album = Injection.Get<IAlbumRepository>().AlbumForId((int)id);
            if (album == null || album.AlbumId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Album not found");
                return;
            }

            // Already disc/track-sorted with artist/genre/art joined in
            IList<Song> songs = album.ListOfSongs();

            SubsonicAlbumWithSongsID3 dto = new SubsonicAlbumWithSongsID3();
            SubsonicMapper.FillAlbumID3(dto, album, null);
            dto.SongCount = songs.Count;
            dto.Duration = songs.Sum(s => s.Duration ?? 0);
            dto.Created = songs.Count > 0 ? SubsonicMapper.Iso8601(songs[0].LastModified) : null;
            dto.Genre = songs.Select(s => s.GenreName).FirstOrDefault(g => g != null);
            dto.Song = songs.Select(SubsonicMapper.ChildFromSong).ToList();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Album = dto;
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetSong(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            Song song = Injection.Get<ISongRepository>().SongForId((int)id);
            if (song == null || song.ItemId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Song not found");
                return;
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Song = SubsonicMapper.ChildFromSong(song);
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetGenres(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IGenreRepository genreRepository = Injection.Get<IGenreRepository>();
            IDictionary<int, GroupCount> songCounts = SubsonicMapper.ToLookup(genreRepository.SongCountsByGenre());
            IDictionary<int, GroupCount> albumCounts = SubsonicMapper.ToLookup(genreRepository.AlbumCountsByGenre());

            List<SubsonicGenre> genres = new List<SubsonicGenre>();
            foreach (Genre genre in genreRepository.AllGenres()) {
                if (genre.GenreId == null) {
                    continue;
                }
                GroupCount count;
                genres.Add(new SubsonicGenre {
                    Value = genre.GenreName,
                    SongCount = songCounts.TryGetValue((int)genre.GenreId, out count) ? count.Count : 0,
                    AlbumCount = albumCounts.TryGetValue((int)genre.GenreId, out count) ? count.Count : 0
                });
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Genres = new SubsonicGenres { Genre = genres };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetVideos(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<Video> videos = Injection.Get<IVideoRepository>().AllVideos();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Videos = new SubsonicVideos { Video = videos.Select(SubsonicMapper.ChildFromVideo).ToList() };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetLyrics(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string artist = req.Get("artist");
            string title = req.Get("title");

            SubsonicLyrics lyrics = new SubsonicLyrics { Artist = artist, Title = title };

            if (!String.IsNullOrEmpty(title)) {
                IList<Song> matches = Injection.Get<ISongRepository>().SearchSongs("SongName", title);
                Song match = matches.FirstOrDefault(s =>
                    s.Lyrics != null
                    && (String.IsNullOrEmpty(artist)
                        || String.Equals(s.ArtistName, artist, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(s.AlbumArtistName, artist, StringComparison.OrdinalIgnoreCase)));
                if (match != null) {
                    lyrics.Artist = match.ArtistName ?? match.AlbumArtistName;
                    lyrics.Title = match.SongName;
                    lyrics.Value = match.Lyrics;
                }
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Lyrics = lyrics;
            SubsonicWriter.Write(req, processor, body);
        }
    }
}
