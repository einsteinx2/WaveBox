using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Subsonic.Handlers;

namespace WaveBox.Subsonic {
    // Terminal middleware for the /rest branch: parses the Subsonic endpoint name, merges
    // query/form parameters, authenticates statelessly, and dispatches through an explicit
    // (AOT-safe, no reflection) endpoint table.
    public class SubsonicDispatcher {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(SubsonicDispatcher));

        private class SubsonicEndpoint {
            public Action<SubsonicRequest, HttpContextProcessor, User> Handler;
            public Role MinRole = Role.Test;

            public SubsonicEndpoint(Action<SubsonicRequest, HttpContextProcessor, User> handler, Role minRole = Role.Test) {
                Handler = handler;
                MinRole = minRole;
            }
        }

        private readonly Dictionary<string, SubsonicEndpoint> endpoints = new Dictionary<string, SubsonicEndpoint>(StringComparer.Ordinal);

        public SubsonicDispatcher() {
            // Explicit registration, lowercase keys (endpoint names are matched case-insensitively).
            // Reflection scanning is not NativeAOT-compatible, same as ApiHandlerFactory.

            // System
            this.Register("ping", SubsonicSystemHandlers.Ping);
            this.Register("getlicense", SubsonicSystemHandlers.GetLicense);
            this.Register("getopensubsonicextensions", SubsonicSystemHandlers.GetOpenSubsonicExtensions);
            this.Register("tokeninfo", SubsonicSystemHandlers.TokenInfo);
            this.Register("getscanstatus", SubsonicSystemHandlers.GetScanStatus);
            this.Register("getartistinfo", SubsonicSystemHandlers.GetArtistInfo);
            this.Register("getartistinfo2", SubsonicSystemHandlers.GetArtistInfo2);

            // Browsing
            this.Register("getmusicfolders", SubsonicBrowsingHandlers.GetMusicFolders);
            this.Register("getindexes", SubsonicBrowsingHandlers.GetIndexes);
            this.Register("getmusicdirectory", SubsonicBrowsingHandlers.GetMusicDirectory);
            this.Register("getartists", SubsonicBrowsingHandlers.GetArtists);
            this.Register("getartist", SubsonicBrowsingHandlers.GetArtist);
            this.Register("getalbum", SubsonicBrowsingHandlers.GetAlbum);
            this.Register("getsong", SubsonicBrowsingHandlers.GetSong);
            this.Register("getgenres", SubsonicBrowsingHandlers.GetGenres);
            this.Register("getvideos", SubsonicBrowsingHandlers.GetVideos);
            this.Register("getlyrics", SubsonicBrowsingHandlers.GetLyrics);

            // Media retrieval
            this.Register("getcoverart", SubsonicMediaHandlers.GetCoverArt);
            this.Register("stream", SubsonicMediaHandlers.Stream);
            this.Register("download", SubsonicMediaHandlers.Download);

            // Album/song lists
            this.Register("getalbumlist", SubsonicListHandlers.GetAlbumList);
            this.Register("getalbumlist2", SubsonicListHandlers.GetAlbumList2);
            this.Register("getrandomsongs", SubsonicListHandlers.GetRandomSongs);
            this.Register("getsongsbygenre", SubsonicListHandlers.GetSongsByGenre);
            this.Register("getnowplaying", SubsonicListHandlers.GetNowPlaying);
            this.Register("getstarred", SubsonicListHandlers.GetStarred);
            this.Register("getstarred2", SubsonicListHandlers.GetStarred2);

            // Searching
            this.Register("search2", SubsonicSearchHandlers.Search2);
            this.Register("search3", SubsonicSearchHandlers.Search3);

            // Media annotation (writes require a full user account, same as the legacy API)
            this.Register("star", SubsonicAnnotationHandlers.Star, Role.User);
            this.Register("unstar", SubsonicAnnotationHandlers.Unstar, Role.User);
            this.Register("scrobble", SubsonicAnnotationHandlers.Scrobble, Role.User);

            // Playlists
            this.Register("getplaylists", SubsonicPlaylistHandlers.GetPlaylists);
            this.Register("getplaylist", SubsonicPlaylistHandlers.GetPlaylist);
            this.Register("createplaylist", SubsonicPlaylistHandlers.CreatePlaylist, Role.User);
            this.Register("updateplaylist", SubsonicPlaylistHandlers.UpdatePlaylist, Role.User);
            this.Register("deleteplaylist", SubsonicPlaylistHandlers.DeletePlaylist, Role.User);

            // User management (getUser/changePassword do their own self-or-admin checks)
            this.Register("getuser", SubsonicUserHandlers.GetUser);
            this.Register("getusers", SubsonicUserHandlers.GetUsers, Role.Admin);
            this.Register("changepassword", SubsonicUserHandlers.ChangePassword, Role.User);
            this.Register("createuser", SubsonicUserHandlers.CreateUser, Role.Admin);
            this.Register("updateuser", SubsonicUserHandlers.UpdateUser, Role.Admin);
            this.Register("deleteuser", SubsonicUserHandlers.DeleteUser, Role.Admin);
        }

        private void Register(string name, Action<SubsonicRequest, HttpContextProcessor, User> handler, Role minRole = Role.Test) {
            this.endpoints[name] = new SubsonicEndpoint(handler, minRole);
        }

        public async Task ProcessAsync(HttpContext context) {
            string method = context.Request.Method.ToUpperInvariant();

            if (method != "GET" && method != "POST") {
                context.Response.StatusCode = 405;
                context.Response.Headers["Allow"] = "GET, POST";
                return;
            }

            // Form bodies (OpenSubsonic formPost extension) merge with the query string
            IFormCollection form = null;
            if (context.Request.HasFormContentType) {
                form = await context.Request.ReadFormAsync(context.RequestAborted);
            }

            SubsonicRequest req = new SubsonicRequest(context, form);

            // Handlers are synchronous and may block (transcode tailing), so dispatch on a
            // worker thread rather than tying up the request loop, same as ApiDispatcher
            await Task.Run(() => this.Dispatch(context, req), context.RequestAborted);
        }

        private void Dispatch(HttpContext context, SubsonicRequest req) {
            HttpContextProcessor processor = new HttpContextProcessor(context);
            string name = EndpointName(context.Request.Path.Value);
            string ip = context.Connection.RemoteIpAddress != null ? context.Connection.RemoteIpAddress.ToString() : "unknown";

            try {
                if (String.IsNullOrEmpty(name)) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Unknown endpoint");
                    return;
                }

                SubsonicError authError;
                User user = Injection.Get<SubsonicAuth>().Authenticate(req, out authError);
                if (user == null) {
                    logger.IfInfo(String.Format("[{0}] Subsonic: {1} auth failed ({2})", ip, name, authError.Code));
                    SubsonicWriter.WriteError(req, processor, authError.Code, authError.Message);
                    return;
                }

                SubsonicEndpoint endpoint;
                if (!this.endpoints.TryGetValue(name.ToLowerInvariant(), out endpoint)) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, name + " is not supported by WaveBox");
                    return;
                }

                if (!user.HasPermission(endpoint.MinRole)) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.NotAuthorized, user.UserName + " is not authorized to use " + name);
                    return;
                }

                logger.IfInfo(String.Format("[{0}/{1}@{2}] Subsonic: {3}", user.UserName, req.ClientName, ip, name));

                endpoint.Handler(req, processor, user);
            } catch (Exception e) {
                logger.Error(e);
                try {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Internal error");
                } catch (Exception inner) {
                    logger.Error(inner);
                }
            }
        }

        // "/ping.view" -> "ping"; the branch middleware has already stripped the /rest prefix
        private static string EndpointName(string path) {
            if (String.IsNullOrEmpty(path)) {
                return null;
            }

            string name = path.Trim('/');

            if (name.EndsWith(".view", StringComparison.OrdinalIgnoreCase)) {
                name = name.Substring(0, name.Length - ".view".Length);
            }

            return name;
        }
    }
}
