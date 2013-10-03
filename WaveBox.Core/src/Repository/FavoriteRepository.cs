using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.Model.Repository
{
	public class FavoriteRepository : IFavoriteRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IAlbumArtistRepository albumArtistRepository;
		private readonly IAlbumRepository albumRepository;
		private readonly IArtistRepository artistRepository;
		private readonly IFolderRepository folderRepository;
		private readonly IGenreRepository genreRepository;
		private readonly IPlaylistRepository playlistRepository;
		private readonly ISongRepository songRepository;
		private readonly IVideoRepository videoRepository;
		private readonly IItemRepository itemRepository;

		public FavoriteRepository(IDatabase database, IAlbumArtistRepository albumArtistRepository, IAlbumRepository albumRepository, IArtistRepository artistRepository, IFolderRepository folderRepository, IGenreRepository genreRepository, IPlaylistRepository playlistRepository, ISongRepository songRepository, IVideoRepository videoRepository, IItemRepository itemRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (albumRepository == null)
				throw new ArgumentNullException("albumRepository");
			if (albumArtistRepository == null)
				throw new ArgumentNullException("albumArtistRepository");
			if (artistRepository == null)
				throw new ArgumentNullException("artistRepository");
			if (folderRepository == null)
				throw new ArgumentNullException("folderRepository");
			if (genreRepository == null)
				throw new ArgumentNullException("genreRepository");
			if (playlistRepository == null)
				throw new ArgumentNullException("playlistRepository");
			if (songRepository == null)
				throw new ArgumentNullException("songRepository");
			if (videoRepository == null)
				throw new ArgumentNullException("videoRepository");
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");

			this.database = database;
			this.albumArtistRepository = albumArtistRepository;
			this.albumRepository = albumRepository;
			this.artistRepository = artistRepository;
			this.folderRepository = folderRepository;
			this.genreRepository = genreRepository;
			this.playlistRepository = playlistRepository;
			this.songRepository = songRepository;
			this.videoRepository = videoRepository;
			this.itemRepository = itemRepository;
		}

		public Favorite FavoriteForId(int favoriteId)
		{
			return this.database.GetSingle<Favorite>("SELECT * FROM Favorite WHERE FavoriteId = ?", favoriteId);
		}

		public int? AddFavorite(int favoriteUserId, int favoriteItemId, ItemType? favoriteItemType)
		{
			if (favoriteItemType == null)
			{
				favoriteItemType = itemRepository.ItemTypeForItemId(favoriteItemId);
			}

			int? itemId = itemRepository.GenerateItemId(ItemType.Favorite);
			if (itemId == null)
			{
				return null;
			}

			Favorite fav = new Favorite() { FavoriteId = itemId, FavoriteUserId = favoriteUserId, FavoriteItemId = favoriteItemId, FavoriteItemTypeId = (int)favoriteItemType };
			fav.TimeStamp = DateTime.UtcNow.ToUniversalUnixTimestamp();

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				conn.InsertLogged(fav);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return itemId;
		}

		public void DeleteFavorite(int favoriteId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				conn.Execute("DELETE FROM Favorite WHERE FavoriteId = ?", favoriteId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}
		}

		public IList<Favorite> FavoritesForUserId(int userId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Favorite>("SELECT * FROM Favorite WHERE FavoriteUserId = ?", userId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public IList<Favorite> FavoritesForArtistId(int? artistId, int? userId)
		{
			if (artistId == null)
				throw new ArgumentNullException("artistId");
			else if (userId == null)
				throw new ArgumentNullException("userId");

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Favorite>("SELECT * FROM Favorite LEFT JOIN Song ON Song.ItemId = Favorite.FavoriteItemId WHERE Song.ArtistId = ? AND Favorite.FavoriteUserId = ?", artistId, userId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public IList<Favorite> FavoritesForAlbumArtistId(int? albumArtistId, int? userId)
		{
			if (albumArtistId == null)
				throw new ArgumentNullException("artistId");
			else if (userId == null)
				throw new ArgumentNullException("userId");

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Favorite>("SELECT * FROM Favorite LEFT JOIN Song ON Song.ItemId = Favorite.FavoriteItemId WHERE Song.AlbumArtistId = ? AND Favorite.FavoriteUserId = ?", albumArtistId, userId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public IList<IItem> ItemsForFavorites(IList<Favorite> favorites)
		{
			if (favorites != null)
			{
				IList<IItem> items = new List<IItem>();
				foreach (Favorite fav in favorites)
				{
					switch (fav.FavoriteItemType)
					{
						case ItemType.AlbumArtist:
							items.Add(albumArtistRepository.AlbumArtistForId(fav.FavoriteItemId));
							break;
						case ItemType.Album:
							items.Add(albumRepository.AlbumForId((int)fav.FavoriteItemId));
							break;
						case ItemType.Artist:
							items.Add(artistRepository.ArtistForId(fav.FavoriteItemId));
							break;
						case ItemType.Folder:
							items.Add(folderRepository.FolderForId((int)fav.FavoriteItemId));
							break;
						case ItemType.Genre:
							items.Add(genreRepository.GenreForId((int)fav.FavoriteItemId));
							break;
						case ItemType.Playlist:
							items.Add(playlistRepository.PlaylistForId((int)fav.FavoriteItemId));
							break;
						case ItemType.Song:
							items.Add(songRepository.SongForId((int)fav.FavoriteItemId));
							break;
						case ItemType.Video:
							items.Add(videoRepository.VideoForId((int)fav.FavoriteItemId));
							break;
					}
				}

				return items;
			}

			return null;
		}

		public IList<IItem> ItemsForUserId(int userId)
		{
			IList<Favorite> favorites = FavoritesForUserId(userId);
			return ItemsForFavorites(favorites);
		}
	}
}

