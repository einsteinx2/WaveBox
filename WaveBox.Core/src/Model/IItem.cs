using System;
using System.IO;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public interface IItem
	{
		[JsonIgnore, Ignore]
		ItemType ItemType { get; }
		
		[JsonProperty("itemTypeId"), Ignore]
		int ItemTypeId { get; }
		
		[JsonProperty("itemId"), Ignore]
		int? ItemId { get; set; }
	}
}

