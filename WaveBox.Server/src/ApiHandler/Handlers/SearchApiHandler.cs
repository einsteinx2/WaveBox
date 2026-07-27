using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers {
    public class SearchApiHandler : IApiHandler {
        public string Name { get { return "search"; } }

        // API handler is read-only, so no permissions checks needed
        public bool CheckPermission(User user, string action) {
            return true;
        }

        /// <summary>
        /// Process performs a search for a query with specified types
        /// </summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            // Lists to return as results
            IList<Artist> artists = new List<Artist>();
            IList<AlbumArtist> albumArtists = new List<AlbumArtist>();
            IList<Album> albums = new List<Album>();
            IList<Song> songs = new List<Song>();
            IList<Video> videos = new List<Video>();

            // If no query is provided, error
            if (!uri.Parameters.TryGetValue("query", out string queryParam)) {
                processor.WriteJson(new SearchResponse("No search query provided", artists, albumArtists, albums, songs, videos));
                return;
            }

            // URL decode to strip any URL-encoded characters
            string query = HttpUtility.UrlDecode(queryParam);

            // Ensure query is not blank
            if (query.Length < 1) {
                processor.WriteJson(new SearchResponse("Query cannot be empty", artists, albumArtists, albums, songs, videos));
                return;
            }

            // Check for query field
            string field = null;
            if (uri.Parameters.TryGetValue("field", out string fieldParam)) {
                // Use input field for query
                field = HttpUtility.UrlDecode(fieldParam);
            }

            // Check for exact match parameter
            bool exact = false;
            if (uri.Parameters.TryGetValue("exact", out string exactParam) && exactParam.IsTrue()) {
                exact = true;
            }

            // If a query type is provided...
            if (uri.Parameters.TryGetValue("type", out string typeParam)) {
                // Iterate all comma-separated values in query type
                foreach (string type in typeParam.Split(',')) {
                    // Return results, populating lists depending on parameters specified
                    switch (type) {
                    case "artists":
                        artists = Injection.Get<IArtistRepository>().SearchArtists(field, query, exact);
                        break;
                    case "albumartists":
                        albumArtists = Injection.Get<IAlbumArtistRepository>().SearchAlbumArtists(field, query, exact);
                        break;
                    case "albums":
                        albums = Injection.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
                        break;
                    case "songs":
                        songs = Injection.Get<ISongRepository>().SearchSongs(field, query, exact);
                        break;
                    case "videos":
                        videos = Injection.Get<IVideoRepository>().SearchVideos(field, query, exact);
                        break;
                    default:
                        artists = Injection.Get<IArtistRepository>().SearchArtists(field, query, exact);
                        albums = Injection.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
                        songs = Injection.Get<ISongRepository>().SearchSongs(field, query, exact);
                        videos = Injection.Get<IVideoRepository>().SearchVideos(field, query, exact);
                        break;
                    }
                }
            } else {
                // For no type, provide all types of data
                artists = Injection.Get<IArtistRepository>().SearchArtists(field, query, exact);
                albumArtists = Injection.Get<IAlbumArtistRepository>().SearchAlbumArtists(field, query, exact);
                albums = Injection.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
                songs = Injection.Get<ISongRepository>().SearchSongs(field, query, exact);
                videos = Injection.Get<IVideoRepository>().SearchVideos(field, query, exact);
            }

            // Check for a request to limit/paginate artists, like SQL
            // Note: can be combined with range or all artists
            if (uri.Parameters.TryGetValue("limit", out string limitParam)) {
                string[] limit = limitParam.Split(',');

                // Ensure valid limit was parsed
                if (limit.Length < 1 || limit.Length > 2) {
                    processor.WriteJson(new SearchResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null, null, null, null, null));
                    return;
                }

                // Validate as integers
                int index = 0;
                int duration = Int32.MinValue;
                if (!Int32.TryParse(limit[0], out index)) {
                    processor.WriteJson(new SearchResponse("Parameter 'limit' requires a valid integer start index", null, null, null, null, null));
                    return;
                }

                // Ensure positive index
                if (index < 0) {
                    processor.WriteJson(new SearchResponse("Parameter 'limit' requires a non-negative integer start index", null, null, null, null, null));
                    return;
                }

                // Check for duration
                if (limit.Length == 2) {
                    if (!Int32.TryParse(limit[1], out duration)) {
                        processor.WriteJson(new SearchResponse("Parameter 'limit' requires a valid integer duration", null, null, null, null, null));
                        return;
                    }

                    // Ensure positive duration
                    if (duration < 0) {
                        processor.WriteJson(new SearchResponse("Parameter 'limit' requires a non-negative integer duration", null, null, null, null, null));
                        return;
                    }
                }

                // No duration?  Return just specified number of each item
                if (duration == Int32.MinValue) {
                    artists = artists.Skip(0).Take(index).ToList();
                    albumArtists = albumArtists.Skip(0).Take(index).ToList();
                    albums = albums.Skip(0).Take(index).ToList();
                    songs = songs.Skip(0).Take(index).ToList();
                    videos = videos.Skip(0).Take(index).ToList();
                } else {
                    // Else return items starting at index, and taking duration
                    artists = artists.Skip(index).Take(duration).ToList();
                    albumArtists = albumArtists.Skip(index).Take(duration).ToList();
                    albums = albums.Skip(index).Take(duration).ToList();
                    songs = songs.Skip(index).Take(duration).ToList();
                    videos = videos.Skip(index).Take(duration).ToList();
                }
            }

            // Return all results
            processor.WriteJson(new SearchResponse(null, artists, albumArtists, albums, songs, videos));
            return;
        }
    }
}
