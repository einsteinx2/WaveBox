using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using WaveBox.Static;
using Ninject;
using WaveBox.Model.Repository;

namespace WaveBox.Model
{
	public interface IItem
	{
		[JsonIgnore, IgnoreRead, IgnoreWrite]
		ItemType ItemType { get; }
		
		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		int ItemTypeId { get; }
		
		[JsonProperty("itemId")]
		int? ItemId { get; set; }
	}

	public static class IItemExtension
	{
		public static bool RecordStat(this IItem item, StatType statType, long timestamp)
		{
			return (object)item.ItemId == null ? false : Injection.Kernel.Get<IStatRepository>().RecordStat((int)item.ItemId, statType, timestamp);
		}
	}
}

