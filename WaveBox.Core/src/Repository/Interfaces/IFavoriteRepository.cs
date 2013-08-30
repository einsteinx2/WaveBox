using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IFavoriteRepository
	{
		Favorite FavoriteForId(int favoriteId);
		int? AddFavorite(int favoriteUserId, int favoriteItemId, ItemType? favoriteItemType);
		void DeleteFavorite(int favoriteId);
		IList<Favorite> FavoritesForUserId(int userId);
		IList<IItem> ItemsForFavorites(IList<Favorite> favorites);
		IList<IItem> ItemsForUserId(int userId);
	}
}

