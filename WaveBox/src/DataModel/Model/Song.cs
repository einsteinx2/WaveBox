using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WaveBox.DataModel.Model
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

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
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

		public Song(int songId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name, genre.genre_name FROM song " +
				                                     "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
				                                     "LEFT JOIN album ON song_album_id = album.album_id " +
				                                     "LEFT JOIN genre ON song_genre_id = genre.genre_id " +
				                                     "WHERE song_id = @songid", conn);
				q.AddNamedParam("@songid", songId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Song(string filePath, int? folderId, TagLib.File file)
		{
			// We need to check to make sure the tag isn't corrupt before handing off to this method, anyway, so just feed in the tag
			// file that we checked for corruption.
			//TagLib.File file = TagLib.File.Create(fsFile.FullName);

			ItemId = Item.GenerateItemId(ItemType.Song);
			if (ItemId == null)
			{
				return;
			}

			FileInfo fsFile = new FileInfo(filePath);
			TagLib.Tag tag = file.Tag;
			FolderId = folderId;

			try
			{
				Artist artist = Artist.ArtistForName(tag.FirstPerformer);
				ArtistId = artist.ArtistId;
				ArtistName = artist.ArtistName;
			}
			catch
			{
				ArtistId = null;
				ArtistName = null;
			}

			try
			{
				Album album = Album.AlbumForName(tag.Album, ArtistId, Convert.ToInt32(tag.Year));
				AlbumId = album.AlbumId;
				AlbumName = album.AlbumName;
				ReleaseYear = album.ReleaseYear;
			}
			catch
			{
				AlbumId = null;
				AlbumName = null;
			}

			FileType = FileType.FileTypeForTagLibMimeType(file.MimeType);

			if (FileType == FileType.Unknown)
			{
				if (logger.IsInfoEnabled) logger.Info("\"" + filePath + "\" Unknown file type: " + file.Properties.Description);
			}

			try
			{
				SongName = tag.Title;
			}
			catch
			{
				SongName = null;
			}

			try
			{
				TrackNumber = Convert.ToInt32(tag.Track);
			}
			catch
			{
				TrackNumber = null;
			}

			try
			{
				DiscNumber = Convert.ToInt32(tag.Disc);
			}
			catch
			{
				DiscNumber = null;
			}

			try
			{
				GenreName = tag.FirstGenre;
			}
			catch
			{
				GenreName = null;
			}

			if ((object)GenreName != null)
			{
				// Retreive the genre id
				GenreId = new Genre(GenreName).GenreId;
			}

			Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
			Bitrate = file.Properties.AudioBitrate;
			FileSize = fsFile.Length;
			LastModified = Convert.ToInt64(fsFile.LastWriteTime.Ticks);
			FileName = fsFile.Name;

			// Generate an art id from the embedded art, if it exists
			int? artId = new Art(file).ArtId;

			// If there was no embedded art, use the folder's art
			artId = (object)artId == null ? Art.ArtIdForItemId(FolderId) : artId;

			// Create the art/item relationship
			Art.UpdateArtItemRelationship(artId, ItemId, true);
		}

		public Song(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			try
			{
				ItemId = reader.GetInt32(reader.GetOrdinal("song_id"));

				FolderId = reader.GetInt32OrNull(reader.GetOrdinal("song_folder_id"));
				ArtistId = reader.GetInt32OrNull(reader.GetOrdinal("song_artist_id"));
				ArtistName = reader.GetStringOrNull(reader.GetOrdinal("artist_name"));
				AlbumId = reader.GetInt32OrNull(reader.GetOrdinal("song_album_id"));
				AlbumName = reader.GetStringOrNull(reader.GetOrdinal("album_name"));
				int? fileTypeId = reader.GetInt32OrNull(reader.GetOrdinal("song_file_type_id"));
				if (fileTypeId != null)
				{
					FileType = FileType.FileTypeForId((int)fileTypeId);
				}
				SongName = reader.GetStringOrNull(reader.GetOrdinal("song_name"));
				TrackNumber = reader.GetInt32OrNull(reader.GetOrdinal("song_track_num"));
				DiscNumber = reader.GetInt32OrNull(reader.GetOrdinal("song_disc_num"));
				Duration = reader.GetInt32OrNull(reader.GetOrdinal("song_duration"));
				Bitrate = reader.GetInt32OrNull(reader.GetOrdinal("song_bitrate"));
				FileSize = reader.GetInt64OrNull(reader.GetOrdinal("song_file_size"));
				LastModified = reader.GetInt64OrNull(reader.GetOrdinal("song_last_modified"));
				FileName = reader.GetStringOrNull(reader.GetOrdinal("song_file_name"));
				ReleaseYear = reader.GetInt32OrNull(reader.GetOrdinal("song_release_year"));
				GenreId = reader.GetInt32OrNull(reader.GetOrdinal("song_genre_id"));
				GenreName = reader.GetStringOrNull(reader.GetOrdinal("genre_name"));
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		public override void InsertMediaItem()
		{			
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("REPLACE INTO song (song_id, song_folder_id, song_artist_id, song_album_id, song_file_type_id, song_name, song_track_num, song_disc_num, song_duration, song_bitrate, song_file_size, song_last_modified, song_file_name, song_release_year, song_genre_id) " + 
													 "VALUES (@songid, @folderid, @artistid, @albumid, @filetype, @songname, @tracknum, @discnum, @duration, @bitrate, @filesize, @lastmod, @filename, @releaseyear, @genreid)"
													 , conn);

				q.AddNamedParam("@songid", ItemId);
				q.AddNamedParam("@folderid", FolderId);
				q.AddNamedParam("@artistid", ArtistId);
				q.AddNamedParam("@albumid", AlbumId);
				q.AddNamedParam("@filetype", (int)FileType);
				q.AddNamedParam("@songname", SongName);
				q.AddNamedParam("@tracknum", TrackNumber);
				q.AddNamedParam("@discnum", DiscNumber);
				q.AddNamedParam("@duration", Duration);
				q.AddNamedParam("@bitrate", Bitrate);
				q.AddNamedParam("@filesize", FileSize);
				q.AddNamedParam("@lastmod", LastModified);
				q.AddNamedParam("@filename", FileName);
				if (ReleaseYear == null)
				{
					q.AddNamedParam("@releaseyear", DBNull.Value);
				}
				else
				{
					q.AddNamedParam("@releaseyear", ReleaseYear);
				}
				q.AddNamedParam("@genreid", GenreId);

				q.Prepare();

				q.ExecuteNonQueryLogged();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			Art.UpdateArtItemRelationship(ArtId, ItemId, true);
			Art.UpdateArtItemRelationship(ArtId, AlbumId, true);
			Art.UpdateArtItemRelationship(ArtId, FolderId, false); // Only update a folder art relationship if it has no folder art
		}

		public static List<Song> AllSongs()
		{
			List<Song> allsongs = new List<Song>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name, genre.genre_name FROM song " +
				                                     "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
				                                     "LEFT JOIN album ON song_album_id = album.album_id " +
				                                     "LEFT JOIN genre ON song_genre_id = genre.genre_id"
				                                     , conn);

				q.Prepare();
				reader = q.ExecuteReader();

				//	Stopwatch sw = new Stopwatch();
				while (reader.Read())
				{
					//sw.Start();
					allsongs.Add(new Song(reader));
					//if (logger.IsInfoEnabled) logger.Info("Elapsed: {0}ms", sw.ElapsedMilliseconds);
					//sw.Restart();
				}
				//	sw.Stop();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return allsongs;
		}

		public static int? CountSongs()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			int? count = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(song_id) FROM song", conn);
				count = Convert.ToInt32(q.ExecuteScalar());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return count;
		}

		public static long? TotalSongSize()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			long? total = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT sum(song_file_size) FROM song", conn);
				total = Convert.ToInt64(q.ExecuteScalar());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return total;
		}

		public static List<Song> SearchSong(string query)
		{
			List<Song> result = new List<Song>();

			if (query == null)
			{
				return result;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM Song WHERE Song_name LIKE @songname", conn);
				q.AddNamedParam("@songname", "%" + query + "%");
				q.Prepare();
				reader = q.ExecuteReader();

				Song s;

				while (reader.Read())
				{
					s = new Song(reader);
					result.Add(s);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return result;
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

		public static bool SongNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? songId)
		{
			// We don't need to instantiate another folder to know what the folder id is.  This should be known when the method is called.
			//Stopwatch sw = new Stopwatch();
			string fileName = Path.GetFileName(filePath);
			long lastModified = Convert.ToInt64(System.IO.File.GetLastWriteTime(filePath).Ticks);
			bool needsUpdating = true;
			isNew = true;
			songId = null;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				// Turns out that COUNT(*) on large tables is REALLY slow in SQLite because it does a full table search.  I created an index on folder_id(because weirdly enough,
				// even though it's a primary key, SQLite doesn't automatically make one!  :O).  We'll pull that, and if we get a row back, then we'll know that this thing exists.

				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song_id, song_last_modified, song_file_size " +
													 "FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename", conn); //AND song_file_size = @filesize", conn);
				//IDbCommand q = Database.GetDbCommand("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod", conn);

				q.AddNamedParam("@folderid", folderId);
				q.AddNamedParam("@filename", fileName);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					isNew = false;

					songId = reader.GetInt32(0);
					long lastModDb = reader.GetInt64(1);
					if (lastModDb == lastModified)
					{
						needsUpdating = false;
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return needsUpdating;
		}
	}
}
