using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using MediaFerry.DataModel.Model;
using MediaFerry.DataModel.Singletons;
using System.IO;
using TagLib;
using Newtonsoft.Json;

namespace MediaFerry.DataModel.Model
{
	public class Song : MediaItem
	{
		[JsonProperty("itemTypeId")]
		new public int ItemTypeId
		{
			get
			{
				return ItemType.SONG.getItemTypeId();
			}
		}

		private int _artistId;
		[JsonProperty("artistId")]
		public int ArtistId
		{
			get
			{
				return _artistId;
			}
			set
			{
				_artistId = value;
			}
		}

		private string _artistName;
		[JsonProperty("artistName")]
		public string ArtistName
		{
			get
			{
				return _artistName;
			}
			set
			{
				_artistName = value;
			}
		}

		private int _albumId;
		[JsonProperty("albumId")]
		public int AlbumId
		{
			get
			{
				return _albumId;
			}
			set
			{
				_albumId = value;
			}
		}

		private string _albumName;
		[JsonProperty("albumName")]
		public string AlbumName
		{
			get
			{
				return _albumName;
			}
			set
			{
				_albumName = value;
			}
		}

		private string _songName;
		[JsonProperty("songName")]
		public string SongName
		{
			get
			{
				return _songName;
			}
			set
			{
				_songName = value;
			}
		}

		private int _trackNumber;
		[JsonProperty("trackNumber")]
		public int TrackNumber
		{
			get
			{
				return _trackNumber;
			}
			set
			{
				_trackNumber = value;
			}
		}

		private int _discNumber;
		[JsonProperty("discNumber")]
		public int DiscNumber
		{
			get
			{
				return _discNumber;
			}
			set
			{
				_discNumber = value;
			}
		}

		private int _releaseYear;
		[JsonProperty("releaseYear")]
		public int ReleaseYear
		{
			get
			{
				return _releaseYear;
			}
			set
			{
				_releaseYear = value;
			}
		}

		public Song()
		{
		}

		public Song(int songId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT song.*, item_type_art.art_id, artist.artist_name, album.album_name FROM song " +
										 "LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND item_id = song_id " +
										 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
										 "LEFT JOIN album ON song_album_id = album.album_id " +
										 "WHERE song_id = @songid");
				q.Parameters.AddWithValue("@itemtypeid", ItemTypeId);
				q.Parameters.AddWithValue("@songid", songId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public Song(System.IO.FileInfo fsFile, int folderId)
		{
            var file = TagLib.File.Create(fsFile.FullName);

			var tag = file.Tag;
            var lol = file.Properties.Codecs;
			_folderId = folderId;

			try
			{
				var artist = Artist.artistForName(tag.FirstPerformer);
                _artistId = artist.ArtistId;
                _artistName = artist.ArtistName;
			}
			catch
			{
                _artistId = 0;
                _artistName = null;
			}

            try
            {
                var album = Album.albumForName(tag.Album, ArtistId);
                _albumId = album.AlbumId;
                _albumName = album.AlbumName;
            }
            catch
            {
                _albumId = 0;
                _albumName = null;
            }

			_fileType = FileType.fileTypeForTagSharpString(file.Properties.Description);

			if (FileType == FileType.UNKNOWN)
				Console.WriteLine("Unknown file type!");


            try
            {
                _songName = tag.Title;
            }
            catch
            {
                _songName = null;
            }

            try
            {
                _trackNumber = Convert.ToInt32(tag.Track);
            }
            catch
            {
                _trackNumber = 0;
            }

            try
            {
                _discNumber = Convert.ToInt32(tag.Disc);
            }
            catch
            {
                _discNumber = 0;
            }

			try
			{
				_releaseYear = Convert.ToInt32(tag.Disc);
			}
			catch
			{
				_releaseYear = 0;
			}

            _duration = Convert.ToInt64(file.Properties.Duration.Ticks);
            _bitrate = file.Properties.AudioBitrate;
            _fileSize = fsFile.Length;
            _lastModified = Convert.ToInt64(fsFile.LastWriteTime.Ticks);
            _fileName = fsFile.Name;

            var art = new CoverArt(fsFile);

		}

		public Song(SqlCeDataReader reader)
		{
			_setPropertiesFromQueryResult(reader);
		}

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			try
			{
				_itemId = reader.GetInt32(reader.GetOrdinal("song_id"));
				_folderId = reader.GetInt32(reader.GetOrdinal("song_folder_id"));
				_artistId = reader.GetInt32(reader.GetOrdinal("song_artist_id"));

				if (reader.GetValue(reader.GetOrdinal("artist_name")) == DBNull.Value) _artistName = "";
				else _artistName = reader.GetString(reader.GetOrdinal("artist_name"));

				_albumId = reader.GetInt32(reader.GetOrdinal("song_album_id"));

				if (reader.GetValue(reader.GetOrdinal("album_name")) == DBNull.Value) _albumName = "";
				else _albumName = reader.GetString(reader.GetOrdinal("album_name"));

				_fileType = FileType.fileTypeForId(reader.GetInt32(reader.GetOrdinal("song_file_type_id")));

				if (reader.GetValue(reader.GetOrdinal("song_name")) == DBNull.Value) _songName = "";
				else _songName = reader.GetString(reader.GetOrdinal("song_name"));

				_trackNumber = reader.GetInt32(reader.GetOrdinal("song_track_num"));
				_discNumber = reader.GetInt32(reader.GetOrdinal("song_disc_num"));
				_duration = reader.GetInt64(reader.GetOrdinal("song_duration"));
				_bitrate = reader.GetInt32(reader.GetOrdinal("song_bitrate"));
				_fileSize = reader.GetInt64(reader.GetOrdinal("song_file_size"));
				_lastModified = reader.GetInt64(reader.GetOrdinal("song_last_modified"));
				_fileName = reader.GetString(reader.GetOrdinal("song_file_name"));
				_releaseYear = reader.GetInt32(reader.GetOrdinal("song_release_year"));

				if (reader.GetValue(reader.GetOrdinal("art_id")) == DBNull.Value) _artId = 0;
				else _artId = reader.GetInt32(reader.GetOrdinal("art_id"));
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		public void updateDatabase()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("INSERT INTO song (song_folder_id, song_artist_id, song_album_id, song_file_type_id, song_name, song_track_num, song_disc_num, song_duration, song_bitrate, song_file_size, song_last_modified, song_file_name, song_release_year)" + 
										 "VALUES (@folderid, @artistid, @albumid, @filetype, @songname, @tracknum, @discnum, @duration, @bitrate, @filesize, @lastmod, @filename, @releaseyear)");

				q.Parameters.AddWithValue("@folderid", FolderId);
				q.Parameters.AddWithValue("@artistid", ArtistId);
				q.Parameters.AddWithValue("@albumid", AlbumId);
				q.Parameters.AddWithValue("@filetype", (int)FileType);

				if (SongName == null)
					q.Parameters.AddWithValue("@songname", DBNull.Value);
				else q.Parameters.AddWithValue("@songname", SongName);

				q.Parameters.AddWithValue("@tracknum", TrackNumber);
				q.Parameters.AddWithValue("@discnum", DiscNumber);
				q.Parameters.AddWithValue("@duration", Duration);
				q.Parameters.AddWithValue("@bitrate", Bitrate);
				q.Parameters.AddWithValue("@filesize", FileSize);
				q.Parameters.AddWithValue("@lastmod", LastModified);
				q.Parameters.AddWithValue("@filename", FileName);
				q.Parameters.AddWithValue("@releaseyear", ReleaseYear);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();

				q.Connection = conn;
				q.Prepare();
				int ins = q.ExecuteNonQuery();

				if (ins < 1)
				{
					Console.WriteLine("Problem!"); 
				}

				return;
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public static List<Song> allSongs()
		{
			var allsongs = new List<Song>();
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT song.*, item_type_art.art_id, artist.artist_name, album.album_name FROM song " +
										 "LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND item_id = song_id " +
										 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
										 "LEFT JOIN album ON song_album_id = album.album_id ");
				q.Parameters.AddWithValue("@itemtypeid", new Song().ItemTypeId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					allsongs.Add(new Song(reader));
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return allsongs;
		}

		// stub!
		public static int CompareSongsByDiscAndTrack(Song x, Song y)
		{
			return 1;
			//return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}
	}
}
