using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class MediaItem
	{
		public virtual int ItemTypeId { get { return 0; } }

		[JsonProperty("itemId")]
		public int? ItemId { get; set; }

		[JsonProperty("folderId")]
		public int? FolderId { get; set; }

		[JsonProperty("fileType")]
		public FileType FileType { get; set; }

		[JsonProperty("duration")]
		public int? Duration { get; set; }

		[JsonProperty("bitrate")]
		public int? Bitrate { get; set; }

		[JsonProperty("fileSize")]
		public long? FileSize { get; set; }

		[JsonProperty("lastModified")]
		public long? LastModified { get; set; }
		
		[JsonProperty("fileName")]
		public string FileName { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get { return Art.ArtIdForItemId(FolderId); } }

		[JsonIgnore]
		public string FilePath { get { return new Folder(FolderId).FolderPath + Path.DirectorySeparatorChar + FileName; } }

		[JsonIgnore]
		public FileStream File { get { return new FileStream(FilePath, FileMode.Open, FileAccess.Read); } }


		/// <summary>
		/// Public methods
		/// </summary>

		public void AddToPlaylist(Playlist thePlaylist, int index)
		{
		}

		public static bool FileNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? itemId)
		{
			ItemType type = Item.ItemTypeForFilePath(filePath);

			bool needsUpdating = false;
			isNew = false;
			itemId = null;

			if (type == ItemType.Song)
			{
				needsUpdating = Song.SongNeedsUpdating(filePath, folderId, out isNew, out itemId);
			}

			return needsUpdating;
		}
	
	}
}
