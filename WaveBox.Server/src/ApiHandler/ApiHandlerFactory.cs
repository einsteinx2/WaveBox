using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Core.Extensions;
using WaveBox.Server;
using WaveBox.Static;

namespace WaveBox.ApiHandler {
    public class ApiHandlerFactory : IApiHandlerFactory {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ApiHandlerFactory));

        // List of API handlers keyed by name (explicit list - reflection scanning is not NativeAOT-compatible)
        private List<IApiHandler> apiHandlers;

        /// <summary>
        /// Return the requested IApiHandler object
        /// <summary>
        public IApiHandler CreateApiHandler(string name) {
            // Any API handlers with this name?  If yes, return it.  If no, return null.
            return this.apiHandlers.SingleOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Register all available API handlers with the factory
        /// <summary>
        public void Initialize() {
            this.apiHandlers = new List<IApiHandler> {
                new AlbumArtistsApiHandler(),
                new AlbumsApiHandler(),
                new ArtApiHandler(),
                new ArtistsApiHandler(),
                new DatabaseApiHandler(),
                new ErrorApiHandler(),
                new FanArtThumbnailApiHandler(),
                new FavoriteApiHandler(),
                new FoldersApiHandler(),
                new GenresApiHandler(),
                new LoginApiHandler(),
                new LogoutApiHandler(),
                new NowPlayingApiHandler(),
                new PlaylistsApiHandler(),
                new ScrobbleApiHandler(),
                new SearchApiHandler(),
                new SettingsApiHandler(),
                new SongsApiHandler(),
                new StatsApiHandler(),
                new StatusApiHandler(),
                new StreamApiHandler(),
                new TranscodeApiHandler(),
                new TranscodeHlsApiHandler(),
                new UsersApiHandler(),
                new VideosApiHandler(),
                new WebApiHandler(),
            };

            foreach (IApiHandler handler in this.apiHandlers) {
                logger.IfInfo("Registered API: " + handler.Name + " -> " + handler.GetType());
            }
        }
    }
}
