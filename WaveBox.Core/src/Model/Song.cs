using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Model;
using WaveBox.Static;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class Song : MediaItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly string[] ValidExtensions = { ".mp3", ".m4a", ".flac", ".wv", ".mpc", ".ogg", ".wma" };

		[JsonIgnore]
		public override ItemType ItemType { get { return ItemType.Song; } }

		[JsonProperty("itemTypeId")]
		public override int ItemTypeId { get { return (int)ItemType.Song; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName"), IgnoreWrite]
		public string ArtistName { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName"), IgnoreWrite]
		public string AlbumName { get; set; }

		[JsonProperty("songName")]
		public string SongName { get; set; }

		[JsonProperty("trackNumber")]
		public int? TrackNumber { get; set; }

		[JsonProperty("discNumber")]
		public int? DiscNumber { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		public Song()
		{
		}

		public override void InsertMediaItem()
		{
			ISQLiteConnection conn = null;
			try
			{
				// insert the song into the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.InsertLogged(this, InsertType.Replace);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			Art.UpdateArtItemRelationship(ArtId, ItemId, true);
			Art.UpdateArtItemRelationship(ArtId, AlbumId, true);
			Art.UpdateArtItemRelationship(ArtId, FolderId, false); // Only update a folder art relationship if it has no folder art
		}

		public static IList<Song> SongsForIds(IList<int> songIds)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

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
				conn.Close();
			}

			// We had an exception somehow, so return an empty list
			return new List<Song>();
		}

		public static IList<Song> AllSongs()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
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
				conn.Close();
			}

			// We had an exception somehow, so return an empty list
			return new List<Song>();
		}

		public static int CountSongs()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(ItemId) FROM Song");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			// We had an exception somehow, so return 0
			return 0;
		}

		public static long TotalSongSize()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<long>("SELECT SUM(FileSize) FROM Song");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			// We had an exception somehow, so return 0
			return 0;
		}

		public static long TotalSongDuration()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<long>("SELECT SUM(Duration) FROM Song");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			// We had an exception somehow, so return 0
			return 0;
		}

		public static List<Song> SearchSongs(string field, string query, bool exact = true)
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

				List<Song> songs;
				if (exact)
				{
					// Search for exact match
					songs = conn.Query<Song>("SELECT * FROM Song WHERE " + field + " = ?", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					songs = conn.Query<Song>("SELECT * FROM Song WHERE " + field + " LIKE ?", "%" + query + "%");
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
				conn.Close();
			}

			// We had an exception somehow, so return an empty list
			return new List<Song>();
		}

		public static int CompareSongsByDiscAndTrack(Song x, Song y)
		{
			if (x.DiscNumber == y.DiscNumber && x.TrackNumber == y.TrackNumber) return 0;

			// if the disc numbers are equal, we have to compare by track
			else if (x.DiscNumber == y.DiscNumber) return x.TrackNumber > y.TrackNumber ? 1 : -1;

			// if the disc numbers are not equal, the one with the higher disc number is greater.
			else return x.DiscNumber > y.DiscNumber ? 1 : -1;
		}

		/*
		 * Factory
		 */

		public class Factory
		{
			public Song CreateSong(int songId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					IEnumerable result = conn.DeferredQuery<Song>("SELECT Song.*, Artist.ArtistName, Album.AlbumName, Genre.GenreName FROM Song " +
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
					conn.Close();
				}

				// No record found, so return an empty Song object
				return new Song();
			}
		}
	}
}
