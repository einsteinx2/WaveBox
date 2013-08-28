using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class FavoritesResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("items")]
		public IList<IItem> Items { get; set; }

		[JsonProperty("favorites")]
		public IList<Favorite> Favorites { get; set; }

		public FavoritesResponse(string error, IList<IItem> items, IList<Favorite> favorites)
		{
			Error = error;
			Items = items;
			Favorites = favorites;
		}
	}
}

