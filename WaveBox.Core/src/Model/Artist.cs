using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Static;
using WaveBox.Model.Repository;

namespace WaveBox.Model
{
	public class Artist : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

		public class Factory
		{
			public Artist CreateArtist(int? artistId)
			{
				if (artistId == null)
				{
					return new Artist();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

					var result = conn.DeferredQuery<Artist>("SELECT * FROM Artist WHERE ArtistId = ?", artistId);

					foreach (Artist a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				return new Artist();
			}

			public Artist CreateArtist(string artistName)
			{
				if (artistName == null || artistName == "")
				{
					return new Artist();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Artist>("SELECT * FROM Artist WHERE ArtistName = ?", artistName);

					foreach (Artist a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				Artist artist = new Artist();
				artist.ArtistName = artistName;
				return artist;
			}
		}
	}
}
