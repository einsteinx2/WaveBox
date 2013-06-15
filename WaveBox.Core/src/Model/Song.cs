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
				conn = Database.GetSqliteConnection();
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
				conn = Database.GetSqliteConnection();

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
				conn = Database.GetSqliteConnection();
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
				conn = Database.GetSqliteConnection();
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
				conn = Database.GetSqliteConnection();
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
				conn = Database.GetSqliteConnection();
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
				conn = Database.GetSqliteConnection();

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
			// if one track contains a disc number and the other doesn't, the one with the disc number is greater
			//if (x.DiscNumber == 0 && y.DiscNumber != 0) return 1;
			//else if (x.DiscNumber != 0 && y.DiscNumber == 0) return -1;

			if (x.DiscNumber == y.DiscNumber && x.TrackNumber == y.TrackNumber) return 0;

			// if the disc numbers are equal, we have to compare by track
			else if (x.DiscNumber == y.DiscNumber) return x.TrackNumber > y.TrackNumber ? 1 : -1;

			// if the disc numbers are not equal, the one with the higher disc number is greater.
			else return x.DiscNumber > y.DiscNumber ? 1 : -1;
		}

		public static bool SongNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? itemId)
		{
			// We don't need to instantiate another folder to know what the folder id is.  This should be known when the method is called.
			//Stopwatch sw = new Stopwatch();
			string fileName = Path.GetFileName(filePath);
			long lastModified = System.IO.File.GetLastWriteTime(filePath).ToUniversalUnixTimestamp();
			bool needsUpdating = true;
			isNew = true;
			itemId = null;

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				IEnumerable result = conn.Query<Song>("SELECT * FROM Song WHERE FolderId = ? AND FileName = ? LIMIT 1", folderId, fileName);

				foreach (Song song in result)
				{
					isNew = false;

					itemId = song.ItemId;
					if (song.LastModified == lastModified)
					{
						needsUpdating = false;
					}

					return needsUpdating;
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

			return needsUpdating;
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
					conn = Database.GetSqliteConnection();
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

			public Song CreateSong(string filePath, int? folderId, TagLib.File file)
			{
				// We need to check to make sure the tag isn't corrupt before handing off to this method, anyway, so just feed in the tag
				// file that we checked for corruption.
				//TagLib.File file = TagLib.File.Create(fsFile.FullName);

				int? itemId = Item.GenerateItemId(ItemType.Song);
				if (itemId == null)
				{
					return new Song();
				}

				Song song = new Song();
				song.ItemId = itemId;

				FileInfo fsFile = new FileInfo(filePath);
				TagLib.Tag tag = file.Tag;
				song.FolderId = folderId;

				try
				{
					Artist artist = Artist.ArtistForName(tag.FirstPerformer);
					song.ArtistId = artist.ArtistId;
					song.ArtistName = artist.ArtistName;
				}
				catch (Exception e)
				{
					if (logger.IsErrorEnabled) logger.Error("Error creating artist info for song: ", e);
					song.ArtistId = null;
					song.ArtistName = null;
				}

				try
				{
					Album album = Album.AlbumForName(tag.Album, song.ArtistId, Convert.ToInt32(tag.Year));
					song.AlbumId = album.AlbumId;
					song.AlbumName = album.AlbumName;
					song.ReleaseYear = album.ReleaseYear;
				}
				catch (Exception e)
				{
					if (logger.IsErrorEnabled) logger.Error("Error creating album info for song: ", e);
					song.AlbumId = null;
					song.AlbumName = null;
					song.ReleaseYear = null;
				}

				song.FileType = song.FileType.FileTypeForTagLibMimeType(file.MimeType);

				if (song.FileType == FileType.Unknown)
				{
					if (logger.IsInfoEnabled) logger.Info("\"" + filePath + "\" Unknown file type: " + file.Properties.Description);
				}

				try
				{
					song.SongName = tag.Title;
				}
				catch
				{
					song.SongName = null;
				}

				try
				{
					song.TrackNumber = Convert.ToInt32(tag.Track);
				}
				catch
				{
					song.TrackNumber = null;
				}

				try
				{
					song.DiscNumber = Convert.ToInt32(tag.Disc);
				}
				catch
				{
					song.DiscNumber = null;
				}

				try
				{
					song.GenreName = tag.FirstGenre;
				}
				catch
				{
					song.GenreName = null;
				}

				if ((object)song.GenreName != null)
				{
					// Retreive the genre id
					song.GenreId = new Genre.Factory().CreateGenre(song.GenreName).GenreId;
				}

				song.Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
				song.Bitrate = file.Properties.AudioBitrate;
				song.FileSize = fsFile.Length;
				song.LastModified = fsFile.LastWriteTime.ToUniversalUnixTimestamp();

				song.FileName = fsFile.Name;

				// Generate an art id from the embedded art, if it exists
				int? artId = new Art.Factory().CreateArt(file).ArtId;

				// If there was no embedded art, use the folder's art
				artId = (object)artId == null ? Art.ArtIdForItemId(song.FolderId) : artId;

				// Create the art/item relationship
				Art.UpdateArtItemRelationship(artId, song.ItemId, true);

				return song;
			}
		}
	}
}
