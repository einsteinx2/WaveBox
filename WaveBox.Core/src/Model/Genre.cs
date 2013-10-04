using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;
using Newtonsoft.Json;

namespace WaveBox.Core.Model
{
	public class Genre : IItem, IGroupingItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return GenreId; } set { GenreId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Genre; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("genreId")]
		public int? GenreId { get; set; }

		[JsonProperty("genreName")]
		public string GenreName { get; set; }

		// Currently unused, only to satisfy IItem interface requirements
		[JsonProperty("artId"), IgnoreRead, IgnoreWrite]
		public int? ArtId { get; set; }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public string GroupingName { get { return GenreName; } }

		public IList<Artist> ListOfArtists()
		{
			if (GenreId == null)
			{
				return new List<Artist>();
			}

			return Injection.Kernel.Get<IGenreRepository>().ListOfArtists((int)GenreId);
		}

		public IList<Album> ListOfAlbums()
		{
			if (GenreId == null)
			{
				return new List<Album>();
			}

			return Injection.Kernel.Get<IGenreRepository>().ListOfAlbums((int)GenreId);
		}

		public IList<Song> ListOfSongs()
		{
			if (GenreId == null)
				return new List<Song>();

			return Injection.Kernel.Get<IGenreRepository>().ListOfSongs((int)GenreId);
		}

		public IList<Folder> ListOfFolders()
		{
			if (GenreId == null)
			{
				return new List<Folder>();
			}

			return Injection.Kernel.Get<IGenreRepository>().ListOfFolders((int)GenreId);
		}

		public override string ToString()
		{
			return String.Format("[Genre: GenreId={0}, GenreName={1}]", this.GenreId, this.GenreName);
		}

		public static int CompareGenresByName(Genre x, Genre y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.GenreName, y.GenreName);
		}
	}
}
