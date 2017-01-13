using System;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Model {
    public class Favorite : IItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return FavoriteId; } set { FavoriteId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Favorite; } }

        [JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonProperty("favoriteId")]
        public int? FavoriteId { get; set; }

        [JsonProperty("favoriteUserId")]
        public int? FavoriteUserId { get; set; }

        [JsonProperty("favoriteItemId")]
        public int? FavoriteItemId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType? FavoriteItemType { get { return (ItemType?)FavoriteItemTypeId; } }

        [JsonProperty("favoriteItemTypeId")]
        public int? FavoriteItemTypeId { get; set; }

        [JsonProperty("timestamp")]
        public long? TimeStamp { get; set; }

        // Currently unused, only to satisfy IItem interface requirements
        [JsonProperty("artId"), IgnoreRead, IgnoreWrite]
        public int? ArtId { get; set; }

        public Favorite() {
        }

        public override string ToString() {
            return String.Format("[Favorite: FavoriteId={0}, FavoriteUserId={1}, FavoriteItemId={2}, FavoriteItemType={3}]", this.FavoriteId, this.FavoriteUserId, this.FavoriteItemId, this.FavoriteItemType);
        }
    }
}

