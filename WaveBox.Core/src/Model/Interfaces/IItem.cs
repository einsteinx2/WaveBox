using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    // Serialized polymorphically (no discriminator emitted) so interface-typed members keep
    // writing the runtime type's full shape, matching the old Newtonsoft behavior
    [JsonPolymorphic]
    [JsonDerivedType(typeof(Album))]
    [JsonDerivedType(typeof(AlbumArtist))]
    [JsonDerivedType(typeof(Artist))]
    [JsonDerivedType(typeof(Favorite))]
    [JsonDerivedType(typeof(Folder))]
    [JsonDerivedType(typeof(Genre))]
    [JsonDerivedType(typeof(Playlist))]
    [JsonDerivedType(typeof(MediaItem))]
    [JsonDerivedType(typeof(Song))]
    [JsonDerivedType(typeof(Video))]
    public interface IItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        ItemType ItemType { get; }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        int ItemTypeId { get; }

        [JsonPropertyName("itemId")]
        int? ItemId { get; set; }

        [JsonPropertyName("artId")]
        int? ArtId { get; }
    }

    public static class IItemExtension {
        public static bool RecordStat(this IItem item, StatType statType, long timestamp) {
            return (object)item.ItemId == null ? false : Injection.Get<IStatRepository>().RecordStat((int)item.ItemId, statType, timestamp);
        }
    }
}

