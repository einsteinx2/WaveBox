using System;
using System.IO;
using Newtonsoft.Json;
using WaveBox.DataModel.Model;

namespace WaveBox
{
	public interface IMediaItem
	{
		int ItemTypeId { get; }
		
		[JsonProperty("itemId")]
		int? ItemId { get; set; }
		
		[JsonProperty("folderId")]
		int? FolderId { get; set; }
		
		[JsonProperty("fileType")]
		FileType FileType { get; set; }
		
		[JsonProperty("duration")]
		int? Duration { get; set; }
		
		[JsonProperty("bitrate")]
		int? Bitrate { get; set; }
		
		[JsonProperty("fileSize")]
		long? FileSize { get; set; }
		
		[JsonProperty("lastModified")]
		long? LastModified { get; set; }
		
		[JsonProperty("fileName")]
		string FileName { get; set; }
		
		[JsonProperty("artId")]
		int? ArtId { get; }
		
		[JsonIgnore]
		string FilePath { get; }
		
		[JsonIgnore]
		FileStream File { get; }

		void AddToPlaylist(Playlist thePlaylist, int index);

		void InsertMediaItem();
	}
}

