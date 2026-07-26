using System;
using Microsoft.Extensions.DependencyInjection;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core {
    public static class CoreModule {
        public static IServiceCollection AddWaveBoxCore(this IServiceCollection services) {
            // Repositories
            services.AddSingleton<IAlbumArtistRepository, AlbumArtistRepository>();
            services.AddSingleton<IAlbumRepository, AlbumRepository>();
            services.AddSingleton<IArtRepository, ArtRepository>();
            services.AddSingleton<IArtistRepository, ArtistRepository>();
            services.AddSingleton<IFavoriteRepository, FavoriteRepository>();
            services.AddSingleton<IFolderRepository, FolderRepository>();
            services.AddSingleton<IGenreRepository, GenreRepository>();
            services.AddSingleton<IItemRepository, ItemRepository>();
            services.AddSingleton<IMediaItemRepository, MediaItemRepository>();
            services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
            services.AddSingleton<ISessionRepository, SessionRepository>();
            services.AddSingleton<ISongRepository, SongRepository>();
            services.AddSingleton<IStatRepository, StatRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IVideoRepository, VideoRepository>();
            return services;
        }
    }
}
