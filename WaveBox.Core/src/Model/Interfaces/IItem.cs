using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using WaveBox.Core.Static;
using Ninject;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public interface IItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        ItemType ItemType { get; }

        [JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
        int ItemTypeId { get; }

        [JsonProperty("itemId")]
        int? ItemId { get; set; }

        [JsonProperty("artId")]
        int? ArtId { get; }
    }

    public static class IItemExtension {
        public static bool RecordStat(this IItem item, StatType statType, long timestamp) {
            return (object)item.ItemId == null ? false : Injection.Kernel.Get<IStatRepository>().RecordStat((int)item.ItemId, statType, timestamp);
        }
    }
}

