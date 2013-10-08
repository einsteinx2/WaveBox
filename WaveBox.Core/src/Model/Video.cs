using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model
{
	public class Video : MediaItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly string[] ValidExtensions = { "m4v", "mp4", "mpg", "mkv", "avi" };

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public override ItemType ItemType { get { return ItemType.Video; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public override int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("width")]
		public int? Width { get; set; }

		[JsonProperty("height")]
		public int? Height { get; set; }

		[JsonProperty("aspectRatio")]
		public float? AspectRatio
		{
			get
			{
				if ((object)Width == null || (object)Height == null || Height == 0)
				{
					return null;
				}

				return (float)Width / (float)Height;
			}
		}

		public Video()
		{
		}

		public override void InsertMediaItem()
		{
			// Insert video
			Injection.Kernel.Get<IVideoRepository>().InsertVideo(this, true);

			// Update art relationships
			Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(ArtId, ItemId, true);

			 // Only update a folder art relationship if it has no folder art
			Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(ArtId, FolderId, false);
		}

		public override string ToString()
		{
			return String.Format("[Video: ItemId={0}, FileName={1}]", this.ItemId, this.FileName);
		}

		public static int CompareVideosByFileName(Video x, Video y)
		{
			return x.FileName.CompareTo(y.FileName);
		}
	}
}
