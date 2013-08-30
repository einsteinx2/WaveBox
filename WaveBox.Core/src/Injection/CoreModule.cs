using System;
using Ninject.Modules;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core
{
	public class CoreModule : NinjectModule
	{
		public override void Load() 
		{
			// Repositories
			Bind<IAlbumArtistRepository>().To<AlbumArtistRepository>().InSingletonScope();
			Bind<IAlbumRepository>().To<AlbumRepository>().InSingletonScope();
			Bind<IArtRepository>().To<ArtRepository>().InSingletonScope();
			Bind<IArtistRepository>().To<ArtistRepository>().InSingletonScope();
			Bind<IFavoriteRepository>().To<FavoriteRepository>().InSingletonScope();
			Bind<IFolderRepository>().To<FolderRepository>().InSingletonScope();
			Bind<IGenreRepository>().To<GenreRepository>().InSingletonScope();
			Bind<IItemRepository>().To<ItemRepository>().InSingletonScope();
			Bind<IMediaItemRepository>().To<MediaItemRepository>().InSingletonScope();
			Bind<IPlaylistRepository>().To<PlaylistRepository>().InSingletonScope();
			Bind<ISessionRepository>().To<SessionRepository>().InSingletonScope();
			Bind<ISongRepository>().To<SongRepository>().InSingletonScope();
			Bind<IStatRepository>().To<StatRepository>().InSingletonScope();
			Bind<IUserRepository>().To<UserRepository>().InSingletonScope();
			Bind<IVideoRepository>().To<VideoRepository>().InSingletonScope();
		}
	}
}

