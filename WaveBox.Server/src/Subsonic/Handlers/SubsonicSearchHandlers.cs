using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicSearchHandlers {
        // Folder flavor: artist/album hits come from the folder tree by folder name — children
        // of a media root read as "artists", deeper folders as "albums" (search3 is the tag flavor)
        public static void Search2(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string query = CleanQuery(req.Get("query"));
            if (query == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter query is missing");
                return;
            }

            int artistCount = req.GetInt("artistCount") ?? 20;
            int artistOffset = req.GetInt("artistOffset") ?? 0;
            int albumCount = req.GetInt("albumCount") ?? 20;
            int albumOffset = req.GetInt("albumOffset") ?? 0;
            int songCount = req.GetInt("songCount") ?? 20;
            int songOffset = req.GetInt("songOffset") ?? 0;

            IFolderRepository folderRepository = Injection.Get<IFolderRepository>();
            HashSet<int> mediaRootIds = new HashSet<int>(
                folderRepository.MediaFolders()
                .Where(f => f.FolderId != null)
                .Select(f => (int)f.FolderId));

            IList<Folder> folderHits;
            IList<Song> songs;
            if (query.Length == 0) {
                folderHits = folderRepository.SearchFolders("");
                songs = Injection.Get<ISongRepository>().AllSongs();
            } else {
                folderHits = folderRepository.SearchFolders(query);
                songs = Injection.Get<ISongRepository>().SearchSongs("SongName", query, false);
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.SearchResult2 = new SubsonicSearchResult2 {
                Artist = folderHits
                    .Where(f => f.ParentFolderId != null && mediaRootIds.Contains((int)f.ParentFolderId))
                    .Skip(artistOffset).Take(artistCount)
                    .Select(f => new SubsonicIndexArtist { Id = f.FolderId.ToString(), Name = f.FolderName })
                    .ToList(),
                Album = folderHits
                    .Where(f => f.ParentFolderId == null || !mediaRootIds.Contains((int)f.ParentFolderId))
                    .Skip(albumOffset).Take(albumCount)
                    .Select(SubsonicMapper.ChildFromFolder)
                    .ToList(),
                Song = songs.Skip(songOffset).Take(songCount).Select(SubsonicMapper.ChildFromSong).ToList()
            };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void Search3(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SearchResults results = RunSearch(req, out SubsonicError error);
            if (error != null) {
                SubsonicWriter.WriteError(req, processor, error.Code, error.Message);
                return;
            }

            IDictionary<int, GroupCount> songCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().SongCountsByAlbum());
            IDictionary<int, GroupCount> albumCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().AlbumCountsByAlbumArtist());

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.SearchResult3 = new SubsonicSearchResult3 {
                Artist = results.Artists.Select(a => {
                    GroupCount count;
                    int? albumCount = a.AlbumArtistId != null && albumCounts.TryGetValue((int)a.AlbumArtistId, out count) ? count.Count : (int?)0;
                    return SubsonicMapper.ArtistID3FromAlbumArtist(a, albumCount, false);
                }).ToList(),
                Album = results.Albums.Select(a => SubsonicMapper.AlbumID3FromAlbum(a, songCounts)).ToList(),
                Song = results.Songs.Select(SubsonicMapper.ChildFromSong).ToList()
            };
            SubsonicWriter.Write(req, processor, body);
        }

        private class SearchResults {
            public IList<AlbumArtist> Artists;
            public IList<Album> Albums;
            public IList<Song> Songs;
        }

        // Clients commonly send `foo*` or a quoted query; the repositories do substring LIKE matching
        private static string CleanQuery(string query) {
            return query == null ? null : query.Trim().Trim('"').TrimEnd('*');
        }

        private static SearchResults RunSearch(SubsonicRequest req, out SubsonicError error) {
            error = null;

            string query = CleanQuery(req.Get("query"));
            if (query == null) {
                error = new SubsonicError { Code = SubsonicError.MissingParameter, Message = "Required parameter query is missing" };
                return null;
            }

            int artistCount = req.GetInt("artistCount") ?? 20;
            int artistOffset = req.GetInt("artistOffset") ?? 0;
            int albumCount = req.GetInt("albumCount") ?? 20;
            int albumOffset = req.GetInt("albumOffset") ?? 0;
            int songCount = req.GetInt("songCount") ?? 20;
            int songOffset = req.GetInt("songOffset") ?? 0;

            SearchResults results = new SearchResults();

            if (query.Length == 0) {
                // OpenSubsonic convention: an empty query returns the whole library (offline sync)
                results.Artists = Injection.Get<IAlbumArtistRepository>().AllAlbumArtists().Skip(artistOffset).Take(artistCount).ToList();
                results.Albums = Injection.Get<IAlbumRepository>().AllAlbums().Skip(albumOffset).Take(albumCount).ToList();
                results.Songs = Injection.Get<ISongRepository>().AllSongs().Skip(songOffset).Take(songCount).ToList();
            } else {
                results.Artists = Injection.Get<IAlbumArtistRepository>().SearchAlbumArtists("AlbumArtistName", query, false).Skip(artistOffset).Take(artistCount).ToList();
                results.Albums = Injection.Get<IAlbumRepository>().SearchAlbums("AlbumName", query, false).Skip(albumOffset).Take(albumCount).ToList();
                results.Songs = Injection.Get<ISongRepository>().SearchSongs("SongName", query, false).Skip(songOffset).Take(songCount).ToList();
            }

            return results;
        }
    }
}
