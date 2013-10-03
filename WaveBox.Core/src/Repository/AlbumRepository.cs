using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
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
			return this.database.GetSingle<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"WHERE Album.AlbumId = ?",
			albumId);
		}

		public Album AlbumForName(string albumName, int? albumArtistId)
		{
			return this.database.GetSingle<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"WHERE Album.AlbumName = ? AND Album.AlbumArtistId = ?",
			albumName, albumArtistId);
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
			return this.database.GetList<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"ORDER BY AlbumName COLLATE NOCASE"
			);
		}

		public IList<Album> AllWithNoMusicBrainzId()
		{
			return this.database.GetList<Album>("SELECT * FROM Album WHERE MusicBrainzId IS NULL");
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

			if (exact)
			{
				// Search for exact match
				return this.database.GetList<Album>(
					"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
					"LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
					"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
					"WHERE Album." + field + " = ? ORDER BY AlbumName COLLATE NOCASE",
				query);
			}

			// Search for fuzzy match (containing query)
			return this.database.GetList<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"WHERE Album." + field + " LIKE ? ORDER BY AlbumName COLLATE NOCASE",
			"%" + query + "%");
		}

		public IList<Album> RandomAlbums(int limit = 10)
		{
			return this.database.GetList<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"ORDER BY RANDOM() LIMIT " + limit
			);
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

			return this.database.GetList<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
				"LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"WHERE Album.AlbumName BETWEEN LOWER(?) AND LOWER(?) " +
				"OR Album.AlbumName BETWEEN UPPER(?) AND UPPER(?) " +
				"ORDER BY Album.AlbumName",
			s, en, s, en);
		}

		// Return a list of albums using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<Album> LimitAlbums(int index, int duration = Int32.MinValue)
		{
			string query = "SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Album " +
						   "LEFT JOIN AlbumArtist ON Album.AlbumArtistId = AlbumArtist.AlbumArtistId " +
						   "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
						   "ORDER BY Album.AlbumName " +
						   "LIMIT ? ";

			// Add duration to LIMIT if needed
			if (duration != Int32.MinValue && duration > 0)
			{
				query += ", ?";
			}

			return this.database.GetList<Album>(query, index, duration);
		}
	}
}
