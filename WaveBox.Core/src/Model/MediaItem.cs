using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class MediaItem : IMediaItem, IGroupingItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public virtual ItemType ItemType { get { return ItemType.Unknown; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public virtual int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("itemId")]
        public int? ItemId { get; set; }

        [JsonPropertyName("folderId")]
        public int? FolderId { get; set; }

        [JsonPropertyName("fileType")]
        public FileType FileType { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("bitrate")]
        public int? Bitrate { get; set; }

        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        [JsonPropertyName("lastModified")]
        public long? LastModified { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("genreId")]
        public int? GenreId { get; set; }

        [JsonPropertyName("genreName"), IgnoreWrite]
        public string GenreName { get; set; }

        [JsonPropertyName("artId"), IgnoreWrite]
        public int? ArtId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public virtual string GroupingName { get { return FileName; } }

        /// <summary>
        /// Public methods
        /// </summary>

        public void AddToPlaylist(Playlist thePlaylist, int index) {
        }

        public virtual void InsertMediaItem() {
        }

        public override bool Equals(Object obj) {
            // If parameter is null return false.
            if ((object)obj == null) {
                return false;
            }

            // If parameter cannot be cast to DelayedOperation return false.
            IMediaItem op = obj as IMediaItem;
            if ((object)op == null) {
                return false;
            }

            // Return true if the fields match:
            return Equals(op);
        }

        public bool Equals(IMediaItem op) {
            // If parameter is null return false:
            if ((object)op == null) {
                return false;
            }

            // Return true if they match
            return ItemId.Equals(op.ItemId);
        }

        public override int GetHashCode() {
            return ItemId.GetHashCode();
        }

        public override string ToString() {
            return String.Format("[MediaItem: ItemId={0}, FileName={1}, LastModified={2}]", this.ItemId, this.FileName, this.LastModified);
        }
    }
}
