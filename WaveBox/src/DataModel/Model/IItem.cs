using System;
using System.IO;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public interface IItem
	{
		[JsonIgnore]
		ItemType ItemType { get; }
		
		[JsonProperty("itemTypeId")]
		int ItemTypeId { get; }
		
		[JsonProperty("itemId")]
		int? ItemId { get; set; }
	}
}

