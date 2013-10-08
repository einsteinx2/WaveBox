using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text;
using System.Linq;
using System.Collections;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.Model.Repository
{
	public class SongRepository : ISongRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public SongRepository(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			this.database = database;
		}

		public Song SongForId(int songId)
		{
			return this.database.GetSingle<Song>(
				"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
				"WHERE Song.ItemId = ? LIMIT 1",
			songId);
		}

		public IList<Song> SongsForIds(IList<int> songIds)
		{
			StringBuilder sb = new StringBuilder(
				"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
				"WHERE"
			);

			for (int i = 0; i < songIds.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(" OR");
				}
				sb.Append(" Song.ItemId = ");
				sb.Append(songIds[i]);
			}

			return this.database.GetList<Song>(sb.ToString());
		}

		public bool InsertSong(Song song, bool replace = false)
		{
			int affected = this.database.InsertObject<Song>(song, replace ? InsertType.Replace : InsertType.InsertOrIgnore);

			return affected > 0;
		}

		public IList<Song> AllSongs()
		{
			return this.database.GetList<Song>(
				"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId"
			);
		}

		public int CountSongs()
		{
			return this.database.GetScalar<int>("SELECT COUNT(ItemId) FROM Song");
		}

		public long TotalSongSize()
		{
			// Check if at least 1 song exists, to prevent exception if summing null
			int exists = this.database.GetScalar<int>("SELECT * FROM Song LIMIT 1");
			if (exists > 0)
			{
				return this.database.GetScalar<long>("SELECT SUM(FileSize) FROM Song");
			}

			return 0;
		}

		public long TotalSongDuration()
		{
			// Check if at least 1 song exists, to prevent exception if summing null
			int exists = this.database.GetScalar<int>("SELECT * FROM Song LIMIT 1");
			if (exists > 0)
			{
				return this.database.GetScalar<long>("SELECT SUM(Duration) FROM Song");
			}

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
			if (!new string[] {"ItemId", "FolderId", "ArtistId", "AlbumArtistId", "AlbumId", "FileTypeId",
				"SongName", "TrackNum", "DiscNum", "Duration", "Bitrate", "FileSize",
				"LastModified", "FileName", "ReleaseYear", "GenreId"}.Contains(field))
			{
				// Not a valid search field, so return an empty list
				return new List<Song>();
			}

			IList<Song> songs;
			if (exact)
			{
				// Search for exact match
				songs = this.database.GetList<Song>(
					"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
					"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
					"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
					"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
					"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
					"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
					"WHERE Song." + field + " = ?",
				query);
			}
			else
			{
				// Search for fuzzy match (containing query)
				songs = this.database.GetList<Song>(
					"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
					"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
					"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
					"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
					"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
					"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
					"WHERE Song." + field + " LIKE ?",
				"%" + query + "%");
			}

			List<Song> sortedSongs = songs.ToList();
			sortedSongs.Sort(Song.CompareSongsByDiscAndTrack);
			return sortedSongs;
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

			IList<Song> songs;
			songs = this.database.GetList<Song>(
				"SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
				"WHERE Song.SongName BETWEEN LOWER(?) AND LOWER(?) " +
				"OR Song.SongName BETWEEN UPPER(?) AND UPPER(?)",
			s, en, s, en);

			List<Song> sortedSongs = songs.ToList();
			sortedSongs.Sort(Song.CompareSongsByDiscAndTrack);
			return sortedSongs;
		}

		// Return a list of songs using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<Song> LimitSongs(int index, int duration = Int32.MinValue)
		{
			// Begin building query
			IList<Song> songs;
			string query = "SELECT Song.*, Artist.ArtistName, AlbumArtist.AlbumArtistName, Album.AlbumName, Genre.GenreName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				"LEFT JOIN AlbumArtist ON Song.AlbumArtistId = AlbumArtist.AlbumArtistId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN Genre ON Song.GenreId = Genre.GenreId " +
				"LEFT JOIN ArtItem ON Song.ItemId = ArtItem.ItemId " +
				"LIMIT ? ";

			// Add duration to LIMIT if needed
			if (duration != Int32.MinValue && duration > 0)
			{
				query += ", ?";
			}

			// Run query, sort, send it back
			songs = this.database.GetList<Song>(query, index, duration);

			List<Song> sortedSongs = songs.ToList();
			sortedSongs.Sort(Song.CompareSongsByDiscAndTrack);
			return sortedSongs;
		}
	}
}
