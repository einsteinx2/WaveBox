using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;

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
}

