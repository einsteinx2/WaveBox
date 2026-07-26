using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class FavoritesResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("items")]
        public IList<IItem> Items { get; set; }

        [JsonPropertyName("favorites")]
        public IList<Favorite> Favorites { get; set; }

        public FavoritesResponse(string error, IList<IItem> items, IList<Favorite> favorites) {
            Error = error;
            Items = items;
            Favorites = favorites;
        }
    }
}

