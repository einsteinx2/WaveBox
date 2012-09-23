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
	public class MediaItem : IMediaItem
	{
		public virtual int ItemTypeId { get { return (int)ItemType.Unknown; } }

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
		public int? ArtId { get { return Art.ArtIdForItemId(ItemId); } }

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

		public virtual void InsertMediaItem()
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
			else if (type == ItemType.Video)
			{
				needsUpdating = Video.VideoNeedsUpdating(filePath, folderId, out isNew, out itemId);
			}

			return needsUpdating;
		}

		public override bool Equals(Object obj)
		{
			// If parameter is null return false.
			if ((object)obj == null)
			{
				return false;
			}
			
			// If parameter cannot be cast to DelayedOperation return false.
			IMediaItem op = obj as IMediaItem;
			if ((object)op == null)
			{
				return false;
			}
			
			// Return true if the fields match:
			return Equals(op);
		}
		
		public bool Equals(IMediaItem op)
		{
			// If parameter is null return false:
			if ((object)op == null)
			{
				return false;
			}
			
			// Return true if they match
			return ItemId.Equals(op.ItemId);
		}
		
		public override int GetHashCode()
		{
			return ItemId.GetHashCode();
		}
	}
}
