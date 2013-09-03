using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using Newtonsoft.Json;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model
{
	public class Album : IItem
	{
		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("albumArtistId")]
		public int? AlbumArtistId { get; set; }

		[JsonProperty("albumArtistName"), IgnoreWrite]
		public string AlbumArtistName { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		[JsonProperty("artId"), IgnoreWrite]
		public int? ArtId { get { return DetermineArtId(); } }

		public Album()
		{
		}

		public int? DetermineArtId()
		{
			// Return the art id (if any) for this album, based on the current best art id using either a song art id or the folder art id
			IList<int> songArtIds = SongArtIds();
			if (songArtIds.Count == 1)
			{
				// There is one unique art ID for these songs, so return it
				return songArtIds[0];
			}
			else
			{
				// Check the folder art id(s)
				IList<int> folderArtIds = FolderArtIds();
				if (folderArtIds.Count == 0)
				{
					// There is no folder art
					if (songArtIds.Count == 0)
					{
						// There is no art for this album
						return null;
					}
					else
					{
						// For now, just return the first art id
						return songArtIds[0];
					}
				}
				else if (folderArtIds.Count == 1)
				{
					// There are multiple different art ids for the songs, but only one folder art, so use that. Likely this is a compilation album
					return folderArtIds[0];
				}
				else
				{
					// There are multiple song and folder art ids, so let's just return the first song art id for now
					return songArtIds[0];
				}
			}
		}

		private IList<int> SongArtIds()
		{
			if (AlbumId == null)
			{
				return new List<int>();
			}

			return Injection.Kernel.Get<IAlbumRepository>().SongArtIds((int)AlbumId);
		}

		private IList<int> FolderArtIds()
		{
			if (AlbumId == null)
				return new List<int>();

			return Injection.Kernel.Get<IAlbumRepository>().FolderArtIds((int)AlbumId);
		}

		public AlbumArtist AlbumArtist()
		{
			return Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForId(AlbumArtistId);
		}

		public IList<Song> ListOfSongs()
		{
			return Injection.Kernel.Get<ISongRepository>().SearchSongs("AlbumId", AlbumId.ToString());
		}

		public override string ToString()
		{
			return String.Format("[Album: ItemId={0}, AlbumName={1}]", this.ItemId, this.AlbumName);
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}
	}
}
