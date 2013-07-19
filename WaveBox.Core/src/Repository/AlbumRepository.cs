using System;
using WaveBox.Model;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using WaveBox.Core.Injection;
using System.Linq;
using WaveBox.Static;
using Ninject;

namespace WaveBox.Model.Repository
{
	public class AlbumRepository : IAlbumRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public AlbumRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public bool InsertAlbum(string albumName, int? artistId, int? releaseYear)
		{
			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Album);
			if (itemId == null)
			{
				return false;
			}

			bool success = false;

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
			}

			return success;
		}

		public Album AlbumForName(string albumName, int? artistId, int? releaseYear = null)
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

		public List<Album> AllAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		public int CountAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(AlbumId) FROM Album");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return 0;
		}

		public List<Album> SearchAlbums(string field, string query, bool exact = true)
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
				conn = database.GetSqliteConnection();

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
				database.CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		public List<Album> RandomAlbums(int limit = 10)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		// Return a list of albums titled between a range of (a-z, A-Z, 0-9 characters)
		public List<Album> RangeAlbums(char start, char end)
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
				conn = database.GetSqliteConnection();

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
				database.CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Album>();
		}

		// Return a list of albums using SQL LIMIT x,y where X is starting index and Y is duration
		public List<Album> LimitAlbums(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

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
				database.CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Album>();
		}
	}
}

