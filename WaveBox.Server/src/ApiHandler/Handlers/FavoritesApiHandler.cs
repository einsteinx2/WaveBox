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

		/// <summary>
		/// Process returns a JSON response list of folders
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			IFavoriteRepository favoriteRepository = Injection.Kernel.Get<IFavoriteRepository>();

			// Try to get the action
			string action = "list";
			if (uri.Parameters.ContainsKey("action"))
			{
				action = uri.Parameters["action"];
			}

			// Try to get the id
			bool success = false;
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(uri.Parameters["id"], out id);
			}

			string error = null;
			IList<IItem> items = new List<IItem>();
			IList<Favorite> favorites = new List<Favorite>();

			switch (action)
			{
				case "list":
					favorites = favoriteRepository.FavoritesForUserId((int)user.UserId);
					items = favoriteRepository.ItemsForFavorites(favorites);
					break;
				case "add":
					if (success)
					{
						favoriteRepository.AddFavorite((int)user.UserId, id, null);
					}
					else
					{
						error = "Missing id parameter";
					}
					break;
				case "delete":
					if (success)
					{
						// Make sure the user is deleting one of their favorites
						Favorite fav = favoriteRepository.FavoriteForId(id);
						if (fav.FavoriteUserId == user.UserId)
						{
							favoriteRepository.DeleteFavorite(id);
						}
						else
						{
							error = "Cannot delete another user's favorite";
						}
					}
					else
					{
						error = "Favorite does not exist";
					}
					break;
				default:
					error = "Invalid action: " + action;
					break;
			}

			// Return all results
			try
			{
				string json = JsonConvert.SerializeObject(new FavoritesResponse(error, items, favorites), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
