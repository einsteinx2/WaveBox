using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class AlbumArtistRepository : IAlbumArtistRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IItemRepository itemRepository;
		private readonly ISongRepository songRepository;

		public AlbumArtistRepository(IDatabase database, IItemRepository itemRepository, ISongRepository songRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");
			if (songRepository == null)
				throw new ArgumentNullException("songRepository");

			this.database = database;
			this.itemRepository = itemRepository;
			this.songRepository = songRepository;
		}

		public AlbumArtist AlbumArtistForId(int? albumArtistId)
		{
			return this.database.GetSingle<AlbumArtist>("SELECT * FROM AlbumArtist WHERE AlbumArtistId = ?", albumArtistId);
		}

		public AlbumArtist AlbumArtistForName(string albumArtistName)
		{
			return this.database.GetSingle<AlbumArtist>("SELECT * FROM AlbumArtist WHERE AlbumArtistName = ?", albumArtistName);
		}

		public IList<AlbumArtist> AllAlbumArtists()
		{
			return this.database.GetList<AlbumArtist>("SELECT * FROM AlbumArtist ORDER BY AlbumArtistName COLLATE NOCASE");
		}

		public IList<AlbumArtist> AllWithNoMusicBrainzId()
		{
			return this.database.GetList<AlbumArtist>("SELECT * FROM AlbumArtist WHERE MusicBrainzId IS NULL");
		}

		public bool InsertAlbumArtist(string albumArtistName, bool replace = false)
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
				int affected = conn.InsertLogged(artist, replace ? InsertType.Replace : InsertType.InsertOrIgnore);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error("Error inserting albumArtist " + albumArtistName, e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}

		public void InsertAlbumArtist(AlbumArtist albumArtist, bool replace = false)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				conn.InsertLogged(albumArtist, replace ? InsertType.Replace : InsertType.InsertOrIgnore);
			}
			catch (Exception e)
			{
				logger.Error("Error inserting albumArtist " + albumArtist, e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}
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

			if (exact)
			{
				// Search for exact match
				return this.database.GetList<AlbumArtist>("SELECT * FROM AlbumArtist WHERE " + field + " = ? ORDER BY AlbumArtistName COLLATE NOCASE", query);
			}

			// Search for fuzzy match (containing query)
			return this.database.GetList<AlbumArtist>("SELECT * FROM AlbumArtist WHERE " + field + " LIKE ? ORDER BY AlbumArtistName COLLATE NOCASE", "%" + query + "%");
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

			// Return the list
			return this.database.GetList<AlbumArtist>(
				"SELECT * FROM AlbumArtist " +
				"WHERE AlbumArtistName BETWEEN LOWER(?) AND LOWER(?) " +
				"OR AlbumArtistName BETWEEN UPPER(?) AND UPPER(?) " +
				"ORDER BY AlbumArtistName",
			s, en, s, en);
		}

		// Return a list of album artists using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<AlbumArtist> LimitAlbumArtists(int index, int duration = Int32.MinValue)
		{
			string query = "SELECT * FROM AlbumArtist ORDER BY AlbumArtistName LIMIT ? ";

			// Add duration to LIMIT if needed
			if (duration != Int32.MinValue && duration > 0)
			{
				query += ", ?";
			}

			return this.database.GetList<AlbumArtist>(query, index, duration);
		}

		// TODO: Rewrite this to be more efficient
		public IList<Song> SinglesForAlbumArtistId(int albumArtistId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				IList<Song> songs;
				songs = conn.Query<Song>("SELECT ItemId FROM Song WHERE AlbumArtistId = ? AND AlbumId IS NULL", albumArtistId);

				if (songs.Count > 0)
				{
					IList<int> songIds = new List<int>();
					foreach (Song song in songs)
					{
						songIds.Add((int)song.ItemId);
					}
					return songRepository.SongsForIds(songIds);
				}
				else
				{
					return new List<Song>();
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

			// We had an exception somehow, so return an empty list
			return new List<Song>();
		}
	}
}

