using System;
using System.Text.Json.Serialization;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Model {
    public class Favorite : IItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return FavoriteId; } set { FavoriteId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Favorite; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("favoriteId")]
        public int? FavoriteId { get; set; }

        [JsonPropertyName("favoriteUserId")]
        public int? FavoriteUserId { get; set; }

        [JsonPropertyName("favoriteItemId")]
        public int? FavoriteItemId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType? FavoriteItemType { get { return (ItemType?)FavoriteItemTypeId; } }

        [JsonPropertyName("favoriteItemTypeId")]
        public int? FavoriteItemTypeId { get; set; }

        [JsonPropertyName("timestamp")]
        public long? TimeStamp { get; set; }

        // Currently unused, only to satisfy IItem interface requirements
        [JsonPropertyName("artId"), IgnoreRead, IgnoreWrite]
        public int? ArtId { get; set; }

        public Favorite() {
        }

        public override string ToString() {
            return String.Format("[Favorite: FavoriteId={0}, FavoriteUserId={1}, FavoriteItemId={2}, FavoriteItemType={3}]", this.FavoriteId, this.FavoriteUserId, this.FavoriteItemId, this.FavoriteItemType);
        }
    }
}

