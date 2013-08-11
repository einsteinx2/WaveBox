using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Linq;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class AlbumArtistRepository : IAlbumArtistRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IItemRepository itemRepository;

		public AlbumArtistRepository(IDatabase database, IItemRepository itemRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");

			this.database = database;
			this.itemRepository = itemRepository;
		}

		public AlbumArtist AlbumArtistForId(int? albumArtistId)
		{
			if (albumArtistId == null)
			{
				return new AlbumArtist();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				var result = conn.DeferredQuery<AlbumArtist>("SELECT * FROM AlbumArtist WHERE AlbumArtistId = ?", albumArtistId);

				foreach (AlbumArtist a in result)
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

			return new AlbumArtist();
		}

		public AlbumArtist AlbumArtistForName(string albumArtistName)
		{
			if (albumArtistName == null || albumArtistName == "")
			{
				return new AlbumArtist();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<AlbumArtist>("SELECT * FROM AlbumArtist WHERE AlbumArtistName = ?", albumArtistName);

				foreach (AlbumArtist a in result)
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

			AlbumArtist artist = new AlbumArtist();
			artist.AlbumArtistName = albumArtistName;
			return artist;
		}

		public bool InsertAlbumArtist(string albumArtistName)
		{
			int? itemId = itemRepository.GenerateItemId(ItemType.AlbumArtist);
			if (itemId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				AlbumArtist artist = new AlbumArtist();
				artist.AlbumArtistId = itemId;
				artist.AlbumArtistName = albumArtistName;
				int affected = conn.InsertLogged(artist, InsertType.InsertOrIgnore);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error("Error inserting artist " + albumArtistName, e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}

		public AlbumArtist AlbumArtistForNameOrCreate(string albumArtistName)
		{
			if (albumArtistName == null || albumArtistName == "")
			{
				return new AlbumArtist();
			}

			// check to see if the artist exists
			AlbumArtist anAlbumArtist = AlbumArtistForName(albumArtistName);

			// if not, create it.
			if (anAlbumArtist.AlbumArtistId == null)
			{
				anAlbumArtist = null;
				if (InsertAlbumArtist(albumArtistName))
				{
					anAlbumArtist = AlbumArtistForNameOrCreate(albumArtistName);
				}
				else 
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the artist id, it will exist this time
					anAlbumArtist = AlbumArtistForName(albumArtistName);
				}
			}

			// then return the artist object retrieved or created.
			return anAlbumArtist;
		}

		public IList<AlbumArtist> AllAlbumArtists()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<AlbumArtist>("SELECT * FROM AlbumArtist ORDER BY AlbumArtistName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<AlbumArtist>();
		}

		public int CountAlbumArtists()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(AlbumArtistId) FROM AlbumArtist");
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

		public IList<AlbumArtist> SearchAlbumArtists(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				return new List<AlbumArtist>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "AlbumArtistName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"AlbumArtistId", "AlbumArtistName"}.Contains(field))
			{
				return new List<AlbumArtist>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				if (exact)
				{
					// Search for exact match
					return conn.Query<AlbumArtist>("SELECT * FROM AlbumArtist WHERE " + field + " = ? ORDER BY AlbumArtistName", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					return conn.Query<AlbumArtist>("SELECT * FROM AlbumArtist WHERE " + field + " LIKE ? ORDER BY AlbumArtistName", "%" + query + "%");
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

			return new List<AlbumArtist>();
		}

		// Return a list of album artists titled between a range of (a-z, A-Z, 0-9 characters)
		public IList<AlbumArtist> RangeAlbumArtists(char start, char end)
		{
			// Ensure characters are alphanumeric, return empty list if either is not
			if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end))
			{
				return new List<AlbumArtist>();
			}

			string s = start.ToString();
			// Add 1 to character to make end inclusive
			string en = Convert.ToChar((int)end + 1).ToString();

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				List<AlbumArtist> albumArtists;
				albumArtists = conn.Query<AlbumArtist>("SELECT * FROM AlbumArtist " +
				                             "WHERE AlbumArtist.AlbumArtistName BETWEEN LOWER(?) AND LOWER(?) " +
				                             "OR AlbumArtist.AlbumArtistName BETWEEN UPPER(?) AND UPPER(?)", s, en, s, en);

				albumArtists.Sort(AlbumArtist.CompareAlbumArtistsByName);
				return albumArtists;
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
			return new List<AlbumArtist>();
		}

		// Return a list of album artists using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<AlbumArtist> LimitAlbumArtists(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Begin building query
				List<AlbumArtist> albumArtists;

				string query = "SELECT * FROM AlbumArtist LIMIT ? ";

				// Add duration to LIMIT if needed
				if (duration != Int32.MinValue && duration > 0)
				{
					query += ", ?";
				}

				// Run query, sort, send it back
				albumArtists = conn.Query<AlbumArtist>(query, index, duration);
				albumArtists.Sort(AlbumArtist.CompareAlbumArtistsByName);
				return albumArtists;
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
			return new List<AlbumArtist>();
		}
	}
}

