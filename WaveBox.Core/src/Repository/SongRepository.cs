using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text;
using System.Linq;
using System.Collections;

namespace WaveBox.Core.Model.Repository
{
	public class SongRepository : ISongRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public SongRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public Song SongForId(int songId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
				                                              "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				                                              "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				                                              "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				                                              "WHERE Song.ItemId = ? LIMIT 1", songId);

				foreach (Song song in result)
				{
					// Record exists, so return it
					return song;
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

			// No record found, so return an empty Song object
			return new Song();
		}

		public IList<Song> SongsForIds(IList<int> songIds)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				StringBuilder sb = new StringBuilder("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
				                                     "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				                                     "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				                                     "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				                                     "WHERE");

				for (int i = 0; i < songIds.Count; i++)
				{
					if (i > 0)
					{
						sb.Append(" OR");
					}
					sb.Append(" Song.ItemId = ");
					sb.Append(songIds[i]);
				}

				return conn.Query<Song>(sb.ToString());
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

		public IList<Song> AllSongs()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
				                        "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				                        "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				                        "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId");
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

		public int CountSongs()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(ItemId) FROM Song");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return 0
			return 0;
		}

		public long TotalSongSize()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Check if at least 1 song exists, to prevent exception if summing null
				int exists = conn.ExecuteScalar<int>("SELECT * FROM Song LIMIT 1");
				if (exists > 0)
				{
					return conn.ExecuteScalar<long>("SELECT SUM(FileSize) FROM Song");
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

			// We had an exception somehow, so return 0
			return 0;
		}

		public long TotalSongDuration()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Check if at least 1 song exists, to prevent exception if summing null
				int exists = conn.ExecuteScalar<int>("SELECT * FROM Song LIMIT 1");
				if (exists > 0)
				{
					return conn.ExecuteScalar<long>("SELECT SUM(Duration) FROM Song");
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

			// We had an exception somehow, so return 0
			return 0;
		}

		public IList<Song> SearchSongs(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				// No query, so return an empty list
				return new List<Song>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "SongName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"ItemId", "FolderId", "ArtistId", "AlbumId", "FileTypeId",
				"SongName", "TrackNum", "DiscNum", "Duration", "Bitrate", "FileSize",
				"LastModified", "FileName", "ReleaseYear", "GenreId"}.Contains(field))
			{
				// Not a valid search field, so return an empty list
				return new List<Song>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				List<Song> songs;
				if (exact)
				{
					// Search for exact match
					songs = conn.Query<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
					                         "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
					                         "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
					                         "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
					                         "WHERE Song." + field + " = ?", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					songs = conn.Query<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
					                         "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
					                         "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
					                         "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
					                         "WHERE Song." + field + " LIKE ?", "%" + query + "%");
				}
				songs.Sort(Song.CompareSongsByDiscAndTrack);
				return songs;
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

		// Return a list of songs titled between a range of (a-z, A-Z, 0-9 characters)
		public IList<Song> RangeSongs(char start, char end)
		{
			// Ensure characters are alphanumeric, return empty list if either is not
			if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end))
			{
				return new List<Song>();
			}

			string s = start.ToString();
			// Add 1 to character to make end inclusive
			string en = Convert.ToChar((int)end + 1).ToString();

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				List<Song> songs;
				songs = conn.Query<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
				                         "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				                         "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				                         "LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				                         "WHERE Song.SongName BETWEEN LOWER(?) AND LOWER(?) " +
				                         "OR Song.SongName BETWEEN UPPER(?) AND UPPER(?)", s, en, s, en);

				songs.Sort(Song.CompareSongsByDiscAndTrack);
				return songs;
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

		// Return a list of songs using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<Song> LimitSongs(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Begin building query
				List<Song> songs;
				string query = "SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
					"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
						"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
						"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
						"LIMIT ? ";

				// Add duration to LIMIT if needed
				if (duration != Int32.MinValue && duration > 0)
				{
					query += ", ?";
				}

				// Run query, sort, send it back
				songs = conn.Query<Song>(query, index, duration);
				songs.Sort(Song.CompareSongsByDiscAndTrack);
				return songs;
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

