using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service;
using WaveBox.Service.Services;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicListHandlers {
        private const int MaxListSize = 500;

        public static void GetAlbumList(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<Album> albums = AlbumsForListType(req, user, out SubsonicError error);
            if (error != null) {
                SubsonicWriter.WriteError(req, processor, error.Code, error.Message);
                return;
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.AlbumList = new SubsonicAlbumList { Album = albums.Select(SubsonicMapper.ChildFromAlbum).ToList() };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetAlbumList2(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<Album> albums = AlbumsForListType(req, user, out SubsonicError error);
            if (error != null) {
                SubsonicWriter.WriteError(req, processor, error.Code, error.Message);
                return;
            }

            IDictionary<int, GroupCount> songCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().SongCountsByAlbum());

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.AlbumList2 = new SubsonicAlbumList2 { Album = albums.Select(a => SubsonicMapper.AlbumID3FromAlbum(a, songCounts)).ToList() };
            SubsonicWriter.Write(req, processor, body);
        }

        private static IList<Album> AlbumsForListType(SubsonicRequest req, User user, out SubsonicError error) {
            error = null;

            string type = req.Get("type");
            if (String.IsNullOrEmpty(type)) {
                error = new SubsonicError { Code = SubsonicError.MissingParameter, Message = "Required parameter type is missing" };
                return null;
            }

            int size = Math.Min(req.GetInt("size") ?? 10, MaxListSize);
            int offset = req.GetInt("offset") ?? 0;
            IAlbumRepository albumRepository = Injection.Get<IAlbumRepository>();

            switch (type.ToLowerInvariant()) {
            case "random":
                return albumRepository.RandomAlbums(size);

            case "newest":
                return albumRepository.NewestAlbums(size, offset);

            case "recent":
                return albumRepository.RecentAlbums(size, offset);

            case "frequent":
                return albumRepository.FrequentAlbums(size, offset);

            case "alphabeticalbyname":
                return albumRepository.AllAlbums().Skip(offset).Take(size).ToList();

            case "alphabeticalbyartist":
                return albumRepository.AllAlbums()
                       .OrderBy(a => a.AlbumArtistName ?? "", StringComparer.OrdinalIgnoreCase)
                       .ThenBy(a => a.AlbumName ?? "", StringComparer.OrdinalIgnoreCase)
                       .Skip(offset).Take(size).ToList();

            case "byyear": {
                int? fromYear = req.GetInt("fromYear");
                int? toYear = req.GetInt("toYear");
                if (fromYear == null || toYear == null) {
                    error = new SubsonicError { Code = SubsonicError.MissingParameter, Message = "Required parameters fromYear and toYear are missing" };
                    return null;
                }
                bool descending = fromYear > toYear;
                int low = Math.Min((int)fromYear, (int)toYear);
                int high = Math.Max((int)fromYear, (int)toYear);
                IEnumerable<Album> byYear = albumRepository.AllAlbums().Where(a => a.ReleaseYear != null && a.ReleaseYear >= low && a.ReleaseYear <= high);
                byYear = descending ? byYear.OrderByDescending(a => a.ReleaseYear) : byYear.OrderBy(a => a.ReleaseYear);
                return byYear.Skip(offset).Take(size).ToList();
            }

            case "bygenre": {
                string genreName = req.Get("genre");
                if (String.IsNullOrEmpty(genreName)) {
                    error = new SubsonicError { Code = SubsonicError.MissingParameter, Message = "Required parameter genre is missing" };
                    return null;
                }
                Genre genre = Injection.Get<IGenreRepository>().GenreForName(genreName);
                if (genre == null || genre.GenreId == null) {
                    return new List<Album>();
                }
                return Injection.Get<IGenreRepository>().ListOfAlbums((int)genre.GenreId).Skip(offset).Take(size).ToList();
            }

            case "starred":
                return StarredItems(user).Albums.Skip(offset).Take(size).ToList();

            case "highest":
                // Ratings don't exist in WaveBox; an empty list keeps client home screens happy
                return new List<Album>();

            default:
                error = new SubsonicError { Code = SubsonicError.Generic, Message = "Unsupported album list type: " + type };
                return null;
            }
        }

        public static void GetRandomSongs(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int size = Math.Min(req.GetInt("size") ?? 10, MaxListSize);
            string genreName = req.Get("genre");
            int? fromYear = req.GetInt("fromYear");
            int? toYear = req.GetInt("toYear");

            IEnumerable<Song> songs;
            if (!String.IsNullOrEmpty(genreName)) {
                Genre genre = Injection.Get<IGenreRepository>().GenreForName(genreName);
                songs = genre == null || genre.GenreId == null
                        ? new List<Song>()
                        : Injection.Get<IGenreRepository>().ListOfSongs((int)genre.GenreId);
            } else {
                songs = Injection.Get<ISongRepository>().AllSongs();
            }

            if (fromYear != null) {
                songs = songs.Where(s => s.ReleaseYear != null && s.ReleaseYear >= fromYear);
            }
            if (toYear != null) {
                songs = songs.Where(s => s.ReleaseYear != null && s.ReleaseYear <= toYear);
            }

            List<Song> pool = songs.ToList();
            List<SubsonicChild> picked = new List<SubsonicChild>();
            Random random = Random.Shared;
            while (picked.Count < size && pool.Count > 0) {
                int index = random.Next(pool.Count);
                picked.Add(SubsonicMapper.ChildFromSong(pool[index]));
                pool.RemoveAt(index);
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.RandomSongs = new SubsonicSongs { Song = picked };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetSongsByGenre(SubsonicRequest req, HttpContextProcessor processor, User user) {
            string genreName = req.Get("genre");
            if (String.IsNullOrEmpty(genreName)) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter genre is missing");
                return;
            }

            int count = Math.Min(req.GetInt("count") ?? 10, MaxListSize);
            int offset = req.GetInt("offset") ?? 0;

            List<SubsonicChild> songs = new List<SubsonicChild>();
            Genre genre = Injection.Get<IGenreRepository>().GenreForName(genreName);
            if (genre != null && genre.GenreId != null) {
                songs = Injection.Get<IGenreRepository>().ListOfSongs((int)genre.GenreId)
                        .Skip(offset).Take(count)
                        .Select(SubsonicMapper.ChildFromSong).ToList();
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.SongsByGenre = new SubsonicSongs { Song = songs };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetNowPlaying(SubsonicRequest req, HttpContextProcessor processor, User user) {
            List<SubsonicNowPlayingEntry> entries = new List<SubsonicNowPlayingEntry>();

            NowPlayingService nowPlayingService = (NowPlayingService)ServiceManager.GetInstance("nowplaying");
            if (nowPlayingService != null) {
                long now = DateTime.UtcNow.ToUnixTime();
                foreach (NowPlaying nowPlaying in nowPlayingService.Playing.ToList()) {
                    SubsonicChild child = SubsonicMapper.ChildFromMediaItem(nowPlaying.MediaItem);
                    if (child == null) {
                        continue;
                    }

                    SubsonicNowPlayingEntry entry = new SubsonicNowPlayingEntry();
                    CopyChild(child, entry);
                    entry.Username = nowPlaying.User != null ? nowPlaying.User.UserName : null;
                    entry.PlayerName = nowPlaying.User != null && nowPlaying.User.CurrentSession != null ? nowPlaying.User.CurrentSession.ClientName : null;
                    entry.MinutesAgo = nowPlaying.StartTime != null ? (int)Math.Max(0, (now - (long)nowPlaying.StartTime) / 60) : (int?)null;
                    entries.Add(entry);
                }
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.NowPlaying = new SubsonicNowPlaying { Entry = entries };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetStarred(SubsonicRequest req, HttpContextProcessor processor, User user) {
            StarredCollections starred = StarredItems(user);

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Starred = new SubsonicStarred {
                Artist = starred.Artists.Select(a => new SubsonicIndexArtist { Id = a.Id, Name = a.Name, Starred = a.Starred }).ToList(),
                Album = starred.Albums.Select(a => WithStarred(SubsonicMapper.ChildFromAlbum(a), starred.StarredDate(a.AlbumId))).ToList(),
                Song = starred.Songs
            };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetStarred2(SubsonicRequest req, HttpContextProcessor processor, User user) {
            StarredCollections starred = StarredItems(user);
            IDictionary<int, GroupCount> songCounts = SubsonicMapper.ToLookup(Injection.Get<IAlbumRepository>().SongCountsByAlbum());

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Starred2 = new SubsonicStarred2 {
                Artist = starred.Artists,
                Album = starred.Albums.Select(a => {
                    SubsonicAlbumID3 dto = SubsonicMapper.AlbumID3FromAlbum(a, songCounts);
                    dto.Starred = starred.StarredDate(a.AlbumId);
                    return dto;
                }).ToList(),
                Song = starred.Songs
            };
            SubsonicWriter.Write(req, processor, body);
        }

        // Shared partitioning of a user's favorites into Subsonic starred buckets
        internal class StarredCollections {
            public List<SubsonicArtistID3> Artists = new List<SubsonicArtistID3>();
            public List<Album> Albums = new List<Album>();
            public List<SubsonicChild> Songs = new List<SubsonicChild>();
            public Dictionary<int, string> StarredDates = new Dictionary<int, string>();

            public string StarredDate(int? itemId) {
                string date;
                return itemId != null && this.StarredDates.TryGetValue((int)itemId, out date) ? date : null;
            }
        }

        internal static StarredCollections StarredItems(User user) {
            StarredCollections result = new StarredCollections();
            if (user.UserId == null) {
                return result;
            }

            foreach (Favorite favorite in Injection.Get<IFavoriteRepository>().FavoritesForUserId((int)user.UserId)) {
                if (favorite.FavoriteItemId == null || favorite.FavoriteItemType == null) {
                    continue;
                }

                int itemId = (int)favorite.FavoriteItemId;
                string starredDate = SubsonicMapper.Iso8601(favorite.TimeStamp ?? DateTime.UtcNow.ToUnixTime());
                result.StarredDates[itemId] = starredDate;

                switch (favorite.FavoriteItemType) {
                case ItemType.Song: {
                    Song song = Injection.Get<ISongRepository>().SongForId(itemId);
                    if (song != null && song.ItemId != null) {
                        result.Songs.Add(WithStarred(SubsonicMapper.ChildFromSong(song), starredDate));
                    }
                    break;
                }
                case ItemType.Video: {
                    Video video = Injection.Get<IVideoRepository>().VideoForId(itemId);
                    if (video != null && video.ItemId != null) {
                        result.Songs.Add(WithStarred(SubsonicMapper.ChildFromVideo(video), starredDate));
                    }
                    break;
                }
                case ItemType.Album: {
                    Album album = Injection.Get<IAlbumRepository>().AlbumForId(itemId);
                    if (album != null && album.AlbumId != null) {
                        result.Albums.Add(album);
                    }
                    break;
                }
                case ItemType.AlbumArtist: {
                    AlbumArtist albumArtist = Injection.Get<IAlbumArtistRepository>().AlbumArtistForId(itemId);
                    if (albumArtist != null && albumArtist.AlbumArtistId != null) {
                        SubsonicArtistID3 dto = SubsonicMapper.ArtistID3FromAlbumArtist(albumArtist, null, false);
                        dto.Starred = starredDate;
                        result.Artists.Add(dto);
                    }
                    break;
                }
                case ItemType.Artist: {
                    Artist artist = Injection.Get<IArtistRepository>().ArtistForId(itemId);
                    if (artist != null && artist.ArtistId != null) {
                        result.Artists.Add(new SubsonicArtistID3 {
                            Id = artist.ArtistId.ToString(),
                            Name = artist.ArtistName,
                            Starred = starredDate
                        });
                    }
                    break;
                }
                }
            }

            return result;
        }

        internal static SubsonicChild WithStarred(SubsonicChild child, string starredDate) {
            child.Starred = starredDate;
            return child;
        }

        internal static void CopyChild(SubsonicChild from, SubsonicChild to) {
            to.Id = from.Id;
            to.Parent = from.Parent;
            to.IsDir = from.IsDir;
            to.Title = from.Title;
            to.Album = from.Album;
            to.Artist = from.Artist;
            to.Track = from.Track;
            to.Year = from.Year;
            to.Genre = from.Genre;
            to.CoverArt = from.CoverArt;
            to.Size = from.Size;
            to.ContentType = from.ContentType;
            to.Suffix = from.Suffix;
            to.Duration = from.Duration;
            to.BitRate = from.BitRate;
            to.Path = from.Path;
            to.IsVideo = from.IsVideo;
            to.DiscNumber = from.DiscNumber;
            to.Created = from.Created;
            to.Starred = from.Starred;
            to.AlbumId = from.AlbumId;
            to.ArtistId = from.ArtistId;
            to.Type = from.Type;
        }
    }
}
