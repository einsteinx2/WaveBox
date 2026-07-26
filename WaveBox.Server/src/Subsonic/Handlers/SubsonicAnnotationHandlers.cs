using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service;
using WaveBox.Service.Services;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicAnnotationHandlers {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(SubsonicAnnotationHandlers));

        public static void Star(SubsonicRequest req, HttpContextProcessor processor, User user) {
            List<int> ids = AllTargetIds(req);
            if (ids.Count == 0) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            IFavoriteRepository favoriteRepository = Injection.Get<IFavoriteRepository>();
            IItemRepository itemRepository = Injection.Get<IItemRepository>();

            // Skip ids that are already starred so repeated star calls don't stack duplicates
            HashSet<int> alreadyStarred = new HashSet<int>(
                favoriteRepository.FavoritesForUserId((int)user.UserId)
                .Where(f => f.FavoriteItemId != null)
                .Select(f => (int)f.FavoriteItemId));

            foreach (int id in ids) {
                if (alreadyStarred.Contains(id)) {
                    continue;
                }
                ItemType itemType = itemRepository.ItemTypeForItemId(id);
                if (itemType == ItemType.Unknown) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "No item exists with id " + id);
                    return;
                }
                favoriteRepository.AddFavorite((int)user.UserId, id, itemType);
            }

            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void Unstar(SubsonicRequest req, HttpContextProcessor processor, User user) {
            List<int> ids = AllTargetIds(req);
            if (ids.Count == 0) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            IFavoriteRepository favoriteRepository = Injection.Get<IFavoriteRepository>();
            IList<Favorite> favorites = favoriteRepository.FavoritesForUserId((int)user.UserId);

            foreach (int id in ids) {
                foreach (Favorite favorite in favorites.Where(f => f.FavoriteItemId == id && f.FavoriteId != null)) {
                    favoriteRepository.DeleteFavorite((int)favorite.FavoriteId);
                }
            }

            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        // star/unstar accept media ids (id), ID3 album ids (albumId), and ID3 artist ids
        // (artistId); the global id space makes them interchangeable
        private static List<int> AllTargetIds(SubsonicRequest req) {
            List<int> ids = new List<int>();
            ids.AddRange(req.GetIntList("id"));
            ids.AddRange(req.GetIntList("albumId"));
            ids.AddRange(req.GetIntList("artistId"));
            return ids;
        }

        public static void Scrobble(SubsonicRequest req, HttpContextProcessor processor, User user) {
            IList<int> ids = req.GetIntList("id");
            if (ids.Count == 0) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            // time values are milliseconds since epoch, parallel to the id values
            IList<string> times = req.GetAll("time");
            bool submission = req.GetBool("submission", true);

            ISongRepository songRepository = Injection.Get<ISongRepository>();
            IStatRepository statRepository = Injection.Get<IStatRepository>();
            NowPlayingService nowPlayingService = (NowPlayingService)ServiceManager.GetInstance("nowplaying");
            List<LfmScrobbleData> lastfmScrobbles = new List<LfmScrobbleData>();

            for (int i = 0; i < ids.Count; i++) {
                Song song = songRepository.SongForId(ids[i]);
                if (song == null || song.ItemId == null) {
                    continue;
                }

                long timestamp = DateTime.UtcNow.ToUnixTime();
                long parsedMs;
                if (i < times.Count && Int64.TryParse(times[i], out parsedMs)) {
                    timestamp = parsedMs / 1000;
                }

                // Register with now playing regardless; a submission also records play stats
                // (song, album, artist, folder — same as the legacy stats endpoint)
                if (nowPlayingService != null && song.Duration != null && song.Duration > 0) {
                    nowPlayingService.Register(user, song, timestamp);
                }

                if (submission) {
                    statRepository.RecordStat((int)song.ItemId, StatType.PLAYED, timestamp);
                    if ((object)song.AlbumId != null) {
                        statRepository.RecordStat((int)song.AlbumId, StatType.PLAYED, timestamp);
                    }
                    if ((object)song.ArtistId != null) {
                        statRepository.RecordStat((int)song.ArtistId, StatType.PLAYED, timestamp);
                    }
                    if ((object)song.FolderId != null) {
                        statRepository.RecordStat((int)song.FolderId, StatType.PLAYED, timestamp);
                    }

                    lastfmScrobbles.Add(new LfmScrobbleData((int)song.ItemId, timestamp));
                }
            }

            // Pass submissions through to Last.fm when the user has linked an account
            if (submission && lastfmScrobbles.Count > 0 && user.LastfmSession != null) {
                Thread lastfmThread = new Thread(() => {
                    try {
                        new Lastfm(user).Scrobble(lastfmScrobbles, LfmScrobbleType.SUBMIT);
                    } catch (Exception e) {
                        logger.Error(e);
                    }
                });
                lastfmThread.Start();
            }

            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }
    }
}
