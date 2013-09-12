using System;
using WaveBox.Core.Model;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class AlbumRepository : IAlbumRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IItemRepository itemRepository;

		public AlbumRepository(IDatabase database, IItemRepository itemRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");

			this.database = database;
			this.itemRepository = itemRepository;
		}

		public Album AlbumForId(int albumId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
													   "LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				                                       "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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
				database.CloseSqliteConnection(conn);
			}

			return new Album();
		}

		public Album AlbumForName(string albumName, int? albumArtistId)
		{
			if (albumName == null || albumName == "" || albumArtistId == null)
			{
				return new Album();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				IEnumerable<Album> result = conn.DeferredQuery<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				                                                      "LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				                                                      "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				                                                      "WHERE Album.AlbumName = ? AND Album.AlbumArtistId = ?", albumName, albumArtistId);
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
				database.CloseSqliteConnection(conn);
			}

			Album album = new Album();
			album.AlbumName = albumName;
			album.AlbumArtistId = albumArtistId;
			return album;
		}

		public bool InsertAlbum(string albumName, int? albumArtistId, int? releaseYear)
		{
			int? itemId = itemRepository.GenerateItemId(ItemType.Album);
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
				album.AlbumArtistId = albumArtistId;
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

		public Album AlbumForName(string albumName, int? albumArtistId, int? releaseYear = null)
		{
			if (albumName == "" || albumName == null || albumArtistId == null)
			{
				return new Album();
			}

			Album a = AlbumForName(albumName, albumArtistId);

			if (a.AlbumId == null)
			{
				a = null;
				if (InsertAlbum(albumName, albumArtistId, releaseYear))
				{
					a = AlbumForName(albumName, albumArtistId, releaseYear);
				}
				else
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the album id, it will exist this time
					a = AlbumForName(albumName, albumArtistId);
				}
			}

			return a;
		}

		public IList<Album> AllAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				                         "LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				                         "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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

		public IList<Album> SearchAlbums(string field, string query, bool exact = true)
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
			if (!new string[] {"ItemId", "AlbumName", "ArtistId", "AlbumArtistId", "ReleaseYear"}.Contains(field))
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
					return conn.Query<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
					                         "LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
					                         "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
					                         "WHERE Album." + field + " = ? ORDER BY AlbumName", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					return conn.Query<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
					                         "LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
					                         "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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

		public IList<Album> RandomAlbums(int limit = 10)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				                         "LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				                         "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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
		public IList<Album> RangeAlbums(char start, char end)
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
				albums = conn.Query<Album>("SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				                           "LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				                           "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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
		public IList<Album> LimitAlbums(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Begin building query
				List<Album> albums;

				string query = "SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
							   "LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
							   "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
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

		public IList<Album> AllWithNoMusicBrainzId()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Album>("SELECT * FROM Album WHERE MusicBrainzId IS NULL");
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

