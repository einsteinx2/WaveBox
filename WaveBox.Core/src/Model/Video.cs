using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class Video : MediaItem {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        public static readonly string[] ValidExtensions = { "m4v", "mp4", "mpg", "mkv", "avi" };

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public override ItemType ItemType { get { return ItemType.Video; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public override int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("aspectRatio")]
        public float? AspectRatio {
            get {
                if ((object)Width == null || (object)Height == null || Height == 0) {
                    return null;
                }

                return (float)Width / (float)Height;
            }
        }

        public Video() {
        }

        public override void InsertMediaItem() {
            // Insert video
            Injection.Get<IVideoRepository>().InsertVideo(this, true);

            // Update art relationships
            Injection.Get<IArtRepository>().UpdateArtItemRelationship(ArtId, ItemId, true);

            // Only update a folder art relationship if it has no folder art
            Injection.Get<IArtRepository>().UpdateArtItemRelationship(ArtId, FolderId, false);
        }

        public override string ToString() {
            return String.Format("[Video: ItemId={0}, FileName={1}]", this.ItemId, this.FileName);
        }

        public static int CompareVideosByFileName(Video x, Video y) {
            return x.FileName.CompareTo(y.FileName);
        }
    }
}
