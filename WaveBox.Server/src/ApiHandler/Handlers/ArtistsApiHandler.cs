using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers {
    class ArtistsApiHandler : IApiHandler {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "artists"; } }

        // API handler is read-only, so no permissions checks needed
        public bool CheckPermission(User user, string action) {
            return true;
        }

        /// <summary>
        /// Process returns an ArtistsResponse containing a list of artists, albums, and songs
        /// </summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            // Lists of artists, albums, songs to be returned via handler
            IList<Artist> artists = new List<Artist>();
            IList<Album> albums = new List<Album>();
            IList<Song> songs = new List<Song>();
            Dictionary<string, int> counts = new Dictionary<string, int>();
            PairList<string, int> sectionPositions = new PairList<string, int>();

            // Optional Last.fm info
            string lastfmInfo = null;

            // Check if an ID was passed
            if (uri.Id != null) {
                // Add artist by ID to the list
                Artist a = Injection.Kernel.Get<IArtistRepository>().ArtistForId((int)uri.Id);
                if (a.ArtistId == null) {
                    processor.WriteJson(new ArtistsResponse("Artist id not valid", null, null, null, null, null, null));
                    return;
                }

                artists.Add(a);

                // Add artist's albums to response
                albums = a.ListOfAlbums();
                counts.Add("albums", albums.Count);

                // If requested, add artist's songs to response
                if (uri.Parameters.ContainsKey("includeSongs") && uri.Parameters["includeSongs"].IsTrue()) {
                    songs = a.ListOfSongs();
                    counts.Add("songs", songs.Count);
                } else {
                    counts.Add("songs", a.ListOfSongs().Count);
                }

                // If requested, add artist's Last.fm info to response
                if (uri.Parameters.ContainsKey("lastfmInfo") && uri.Parameters["lastfmInfo"].IsTrue()) {
                    logger.IfInfo("Querying Last.fm for artist: " + a.ArtistName);
                    try {
                        lastfmInfo = Lastfm.GetArtistInfo(a);
                        logger.IfInfo("Last.fm query complete!");
                    } catch (Exception e) {
                        logger.Error("Last.fm query failed!");
                        logger.Error(e);
                    }
                }

                // Get favorites count
                int numFavorites = Injection.Kernel.Get<IFavoriteRepository>().FavoritesForArtistId(a.ArtistId, user.UserId).Count;
                counts.Add("favorites", numFavorites);
            }
            // Check for a request for range of artists
            else if (uri.Parameters.ContainsKey("range")) {
                string[] range = uri.Parameters["range"].Split(',');

                // Ensure valid range was parsed
                if (range.Length != 2) {
                    processor.WriteJson(new ArtistsResponse("Parameter 'range' requires a valid, comma-separated character tuple", null, null, null, null, null, null));
                    return;
                }

                // Validate as characters
                char start, end;
                if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end)) {
                    processor.WriteJson(new ArtistsResponse("Parameter 'range' requires characters which are single alphanumeric values", null, null, null, null, null, null));
                    return;
                }

                // Grab range of artists
                artists = Injection.Kernel.Get<IArtistRepository>().RangeArtists(start, end);
            }

            // Check for a request to limit/paginate artists, like SQL
            // Note: can be combined with range or all artists
            if (uri.Parameters.ContainsKey("limit") && !uri.Parameters.ContainsKey("id")) {
                string[] limit = uri.Parameters["limit"].Split(',');

                // Ensure valid limit was parsed
                if (limit.Length < 1 || limit.Length > 2 ) {
                    processor.WriteJson(new ArtistsResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null, null, null, null, null, null));
                    return;
                }

                // Validate as integers
                int index = 0;
                int duration = Int32.MinValue;
                if (!Int32.TryParse(limit[0], out index)) {
                    processor.WriteJson(new ArtistsResponse("Parameter 'limit' requires a valid integer start index", null, null, null, null, null, null));
                    return;
                }

                // Ensure positive index
                if (index < 0) {
                    processor.WriteJson(new ArtistsResponse("Parameter 'limit' requires a non-negative integer start index", null, null, null, null, null, null));
                    return;
                }

                // Check for duration
                if (limit.Length == 2) {
                    if (!Int32.TryParse(limit[1], out duration)) {
                        processor.WriteJson(new ArtistsResponse("Parameter 'limit' requires a valid integer duration", null, null, null, null, null, null));
                        return;
                    }

                    // Ensure positive duration
                    if (duration < 0) {
                        processor.WriteJson(new ArtistsResponse("Parameter 'limit' requires a non-negative integer duration", null, null, null, null, null, null));
                        return;
                    }
                }

                // Check if results list already populated by range
                if (artists.Count > 0) {
                    // No duration?  Return just specified number of artists
                    if (duration == Int32.MinValue) {
                        artists = artists.Skip(0).Take(index).ToList();
                    } else {
                        // Else, return artists starting at index, up to count duration
                        artists = artists.Skip(index).Take(duration).ToList();
                    }
                } else {
                    // If no artists in list, grab directly using model method
                    artists = Injection.Kernel.Get<IArtistRepository>().LimitArtists(index, duration);
                }


            }

            // Finally, if no artists already in list and no ID attribute, send the whole list
            if (artists.Count == 0 && uri.Id == null) {
                artists = Injection.Kernel.Get<IArtistRepository>().AllArtists();
                sectionPositions = Utility.SectionPositionsFromSortedList(new List<IGroupingItem>(artists.Select(c => (IGroupingItem)c)));
            }

            // Send it!
            processor.WriteJson(new ArtistsResponse(null, artists, albums, songs, counts, lastfmInfo, sectionPositions));
        }
    }
}
