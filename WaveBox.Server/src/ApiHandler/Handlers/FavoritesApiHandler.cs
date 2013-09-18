using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class FavoriteApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "favorites"; } }

		// Standard permissions
		public bool CheckPermission(User user, string action)
		{
			switch (action)
			{
				// Write
				case "create":
				case "delete":
					return user.HasPermission(Role.User);
				// Read
				case "read":
				default:
					return user.HasPermission(Role.Test);
			}

			return false;
		}

		/// <summary>
		/// Process returns a JSON response list of folders
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Shortcut to favorites repository
			IFavoriteRepository favoriteRepository = Injection.Kernel.Get<IFavoriteRepository>();

			// Lists of favorites and items associated
			IList<IItem> items = new List<IItem>();
			IList<Favorite> favorites = new List<Favorite>();

			// If no action specified, read favorites
			if (uri.Action == null || uri.Action == "read")
			{
				// Get this users's favorites
				favorites = favoriteRepository.FavoritesForUserId((int)user.UserId);

				// Get the items associated with their favorites
				items = favoriteRepository.ItemsForFavorites(favorites);

				// Send response
				processor.WriteJson(new FavoritesResponse(null, items, favorites));
				return;
			}

			// Verify ID present for remaining actions
			if (uri.Id == null)
			{
				processor.WriteJson(new FavoritesResponse("ID required for modifying favorites", null, null));
				return;
			}

			// create - add favorites
			if (uri.Action == "create")
			{
				favoriteRepository.AddFavorite((int)user.UserId, (int)uri.Id, null);
				processor.WriteJson(new FavoritesResponse(null, items, favorites));
				return;
			}

			// delete - remove favorites
			if (uri.Action == "delete")
			{
				// Grab favorite to delete, verify its ownership
				Favorite fav = favoriteRepository.FavoriteForId((int)uri.Id);
				if (fav.FavoriteUserId != user.UserId)
				{
					processor.WriteJson(new FavoritesResponse("Cannot delete another user's favorite", null, null));
					return;
				}

				// Remove favorite
				favoriteRepository.DeleteFavorite((int)uri.Id);
				processor.WriteJson(new FavoritesResponse(null, items, favorites));
				return;
			}

			// Invalid action
			processor.WriteJson(new FavoritesResponse("Invalid action specified: " + uri.Action, null, null));
			return;
		}
	}
}
