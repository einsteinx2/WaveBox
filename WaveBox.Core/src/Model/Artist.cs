using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model
{
	public class Artist : IItem
	{
		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return ArtistId; } set { ArtistId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Artist; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("artId"), IgnoreWrite]
		public int? ArtId { get { return Injection.Kernel.Get<IArtRepository>().ArtIdForItemId(ArtistId); } }

		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public List<Album> ListOfAlbums()
		{
			return Injection.Kernel.Get<IAlbumRepository>().SearchAlbums("ArtistId", ArtistId.ToString());
		}

		public List<Song> ListOfSongs()
		{
			return Injection.Kernel.Get<ISongRepository>().SearchSongs("ArtistId", ArtistId.ToString());
		}

		public static int CompareArtistsByName(Artist x, Artist y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.ArtistName, y.ArtistName);
		}
	}
}
