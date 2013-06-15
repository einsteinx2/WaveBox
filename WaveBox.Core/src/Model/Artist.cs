using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;

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
		public int? ArtId { get { return Art.ArtIdForItemId(ArtistId); } }

		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{

		}

		private static bool InsertArtist(string artistName)
		{
			int? itemId = Item.GenerateItemId(ItemType.Artist);
			if (itemId == null)
			{
				return false;
			}
			
			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				Artist artist = new Artist();
				artist.ArtistId = itemId;
				artist.ArtistName = artistName;
				int affected = conn.InsertLogged(artist, InsertType.InsertOrIgnore);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error("Error inserting artist " + artistName, e);
			}
			finally
			{
				conn.Close();
			}

			return success;
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public List<Album> ListOfAlbums()
		{
			return Album.SearchAlbums("ArtistId", ArtistId.ToString());
		}

		public List<Song> ListOfSongs()
		{
			return Song.SearchSongs("ArtistId", ArtistId.ToString());
		}

		public static Artist ArtistForName(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return new Artist();
			}

			// check to see if the artist exists
			Artist anArtist = new Artist.Factory().CreateArtist(artistName);

			// if not, create it.
			if (anArtist.ArtistId == null)
			{
				anArtist = null;
				if (InsertArtist(artistName))
				{
					anArtist = ArtistForName(artistName);
				}
				else 
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the artist id, it will exist this time
					anArtist = new Artist.Factory().CreateArtist(artistName);
				}
			}

			// then return the artist object retrieved or created.
			return anArtist;
		}

		public static List<Artist> AllArtists()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Artist>("SELECT * FROM Artist ORDER BY ArtistName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Artist>();
		}

		public static int CountArtists()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(ArtistId) FROM Artist", conn);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}

		public static List<Artist> SearchArtists(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				return new List<Artist>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "ArtistName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"ArtistId", "ArtistName"}.Contains(field))
			{
				return new List<Artist>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();

				if (exact)
				{
					// Search for exact match
					return conn.Query<Artist>("SELECT * FROM Artist WHERE " + field + " = ? ORDER BY ArtistName", query);
				}
				else
				{	
					// Search for fuzzy match (containing query)
					return conn.Query<Artist>("SELECT * FROM Artist WHERE " + field + " LIKE ? ORDER BY ArtistName", "%" + query + "%");
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Artist>();
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
					conn = Database.GetSqliteConnection();

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
					conn.Close();
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
					conn = Database.GetSqliteConnection();
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
					conn.Close();
				}

				Artist artist = new Artist();
				artist.ArtistName = artistName;
				return artist;
			}
		}
	}
}
