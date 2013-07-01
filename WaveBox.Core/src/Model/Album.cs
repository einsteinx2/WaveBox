using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using Newtonsoft.Json;
using WaveBox.Core.Injected;
using WaveBox.Static;

namespace WaveBox.Model
{
	public class Album : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName"), IgnoreWrite]
		public string ArtistName { get; set; }

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
			List<int> songArtIds = SongArtIds();
			if (songArtIds.Count == 1)
			{
				// There is one unique art ID for these songs, so return it
				return songArtIds[0];
			}
			else
			{
				// Check the folder art id(s)
				List<int> folderArtIds = FolderArtIds();
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

		private List<int> SongArtIds()
		{
			List<int> songArtIds = new List<int>();

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<Art>("SELECT ArtItem.ArtId FROM Song " +
													 "LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
													 "WHERE Song.AlbumId = ? AND ArtItem.ArtId IS NOT NULL GROUP BY ArtItem.ArtId", AlbumId);
				foreach (Art art in result)
				{
					songArtIds.Add((int)art.ArtId);
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

			return songArtIds;
		}

		private List<int> FolderArtIds()
		{
			List<int> folderArtIds = new List<int>();

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<Art>("SELECT ArtItem.ArtId FROM Song " +
													 "LEFT JOIN ArtItem ON Song.FolderId = ArtItem.ItemId " +
													 "WHERE Song.AlbumId = ? AND ArtItem.ArtId IS NOT NULL GROUP BY ArtItem.ArtId", AlbumId);
				foreach (Art art in result)
				{
					folderArtIds.Add((int)art.ArtId);
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

			return folderArtIds;
		}

		private static bool InsertAlbum(string albumName, int? artistId, int? releaseYear)
		{
			int? itemId = Item.GenerateItemId(ItemType.Album);
			if (itemId == null)
			{
				return false;
			}

			bool success = false;

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				Album album = new Album();
				album.AlbumId = itemId;
				album.AlbumName = albumName;
				album.ArtistId = artistId;
				album.ReleaseYear = releaseYear;
				success = conn.InsertLogged(album, InsertType.InsertOrIgnore) > 0;
			}
			catch (Exception e)
			{
				logger.Error("Error inserting album " + albumName, e);
				success = false;
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
		}

		public Artist Artist()
		{
			return new Artist.Factory().CreateArtist(ArtistId);
		}

		public List<Song> ListOfSongs()
		{
			return Song.SearchSongs("AlbumId", AlbumId.ToString());
		}

		public static Album AlbumForName(string albumName, int? artistId, int? releaseYear = null)
		{
			if (albumName == "" || albumName == null || artistId == null)
			{
				return new Album();
			}

			Album a = new Album.Factory().CreateAlbum(albumName, artistId);

			if (a.AlbumId == null)
			{
				a = null;
				if (InsertAlbum(albumName, artistId, releaseYear))
				{
					a = AlbumForName(albumName, artistId, releaseYear);
				}
				else
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the album id, it will exist this time
					a = new Album.Factory().CreateAlbum(albumName, artistId);
				}
			}

			return a;
		}

		public static List<Album> AllAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
										"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
										"ORDER BY AlbumName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		public static int CountAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(AlbumId) FROM Album");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return 0;
		}

		public static List<Album> SearchAlbums(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				return new List<Album>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "AlbumName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"ItemId", "AlbumName", "ArtistId", "ReleaseYear"}.Contains(field))
			{
				return new List<Album>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

				if (exact)
				{
					// Search for exact match
					return conn.Query<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
											"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
											"WHERE Album." + field + " = ? ORDER BY AlbumName", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					return conn.Query<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
											"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
											"WHERE Album." + field + " LIKE ? ORDER BY AlbumName", "%" + query + "%");
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

			return new List<Album>();
		}

		public static List<Album> RandomAlbums(int limit = 10)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
										"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
										"ORDER BY RANDOM() LIMIT " + limit);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		// Return a list of albums titled between a range of (a-z, A-Z, 0-9 characters)
		public static List<Album> RangeAlbums(char start, char end)
		{
			// Ensure characters are alphanumeric, return empty list if either is not
			if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end))
			{
				return new List<Album>();
			}

			string s = start.ToString();
			// Add 1 to character to make end inclusive
			string en = Convert.ToChar((int)end + 1).ToString();

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

				List<Album> albums;
				albums = conn.Query<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
										"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
										"WHERE Album.AlbumName BETWEEN LOWER(?) AND LOWER(?) " +
										"OR Album.AlbumName BETWEEN UPPER(?) AND UPPER(?)", s, en, s, en);

				albums.Sort(Album.CompareAlbumsByName);
				return albums;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Album>();
		}

		// Return a list of albums using SQL LIMIT x,y where X is starting index and Y is duration
		public static List<Album> LimitAlbums(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

				// Begin building query
				List<Album> albums;

				string query = "SELECT Album.*, Artist.ArtistName FROM Album " +
								"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
								"LIMIT ? ";

				// Add duration to LIMIT if needed
				if (duration != Int32.MinValue && duration > 0)
				{
					query += ", ?";
				}

				// Run query, sort, send it back
				albums = conn.Query<Album>(query, index, duration);
				albums.Sort(Album.CompareAlbumsByName);
				return albums;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Album>();
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}

		public class Factory
		{
			public Factory()
			{
			}

			public Album CreateAlbum(int albumId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
															"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
															"WHERE Album.AlbumId = ?", albumId);

					foreach (Album a in result)
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

				return new Album();
			}

			public Album CreateAlbum(string albumName, int? artistId)
			{
				if (albumName == null || albumName == "")
				{
					return new Album();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
															"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
															"WHERE Album.AlbumName = ? AND Album.ArtistId = ?", albumName, artistId);

					foreach (Album a in result)
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

				Album album = new Album();
				album.AlbumName = albumName;
				album.ArtistId = artistId;
				return album;
			}
		}
	}
}
