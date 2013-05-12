using System;
using System.IO;
using Newtonsoft.Json;

namespace WaveBox.Model
{
	public interface IMediaItem : IItem
	{
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

		[JsonProperty("genreId")]
		int? GenreId { get; set; }

		[JsonProperty("genreName")]
		string GenreName { get; set; }

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

