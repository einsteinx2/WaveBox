using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IFavoriteRepository
	{
		Favorite FavoriteForId(int favoriteId);
		int? AddFavorite(int favoriteUserId, int favoriteItemId, ItemType? favoriteItemType);
		bool DeleteFavorite(int favoriteId);
		IList<Favorite> FavoritesForUserId(int userId);
		IList<Favorite> FavoritesForArtistId(int? artistId, int? userId);
		IList<Favorite> FavoritesForAlbumArtistId(int? albumArtistId, int? userId);
		IList<IItem> ItemsForFavorites(IList<Favorite> favorites);
		IList<IItem> ItemsForUserId(int userId);
	}
}

