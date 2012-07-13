using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
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
		[JsonProperty("itemTypeId")]
		public override int ItemTypeId
		{
			get
			{
				return (int)ItemType.SONG;
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
				var q = new SqlCeCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
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
				Console.WriteLine("[SONG] " + e.ToString());
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
				Console.WriteLine("[SONG] " + "Unknown file type: " + file.Properties.Description);

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
				_releaseYear = Convert.ToInt32(tag.Year);
			}
			catch
			{
				_releaseYear = 0;
			}

            _duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
            _bitrate = file.Properties.AudioBitrate;
            _fileSize = fsFile.Length;
            _lastModified = Convert.ToInt64(fsFile.LastWriteTime.Ticks);
            _fileName = fsFile.Name;

			// check to see if the folder has art associated with it.  if it does, use that art.  if not,
			// check to see if the tag contains art.
			_artId = new Folder(folderId).ArtId;

			if (_artId == 0)
			{
				var art = new CoverArt(fsFile);
				_artId = art.ArtId;
			}


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
				_duration = reader.GetInt32(reader.GetOrdinal("song_duration"));
				_bitrate = reader.GetInt32(reader.GetOrdinal("song_bitrate"));
				_fileSize = reader.GetInt64(reader.GetOrdinal("song_file_size"));
				_lastModified = reader.GetInt64(reader.GetOrdinal("song_last_modified"));
				_fileName = reader.GetString(reader.GetOrdinal("song_file_name"));
				_releaseYear = reader.GetInt32(reader.GetOrdinal("song_release_year"));

				if (reader.GetValue(reader.GetOrdinal("song_art_id")) == DBNull.Value) _artId = 0;
				else _artId = reader.GetInt32(reader.GetOrdinal("song_art_id"));
				//_artId = 0;
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG] " + e.ToString());
			}
		}

		public void updateDatabase()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				// insert the song into the database
				var q = new SqlCeCommand("INSERT INTO song (song_folder_id, song_artist_id, song_album_id, song_file_type_id, song_name, song_track_num, song_disc_num, song_duration, song_bitrate, song_file_size, song_last_modified, song_file_name, song_release_year, song_art_id)" + 
										 "VALUES (@folderid, @artistid, @albumid, @filetype, @songname, @tracknum, @discnum, @duration, @bitrate, @filesize, @lastmod, @filename, @releaseyear, @artid)");

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
				q.Parameters.AddWithValue("@artid", ArtId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();

				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();
				return;
			}

			catch (Exception e)
			{
				Console.WriteLine("[SONG] " + e.ToString());
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
				var q = new SqlCeCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
										 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
										 "LEFT JOIN album ON song_album_id = album.album_id ");

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

			//	var sw = new Stopwatch();
				while (reader.Read())
				{
			//		sw.Start();
					allsongs.Add(new Song(reader));
			//		Console.WriteLine("Elapsed: {0}ms", sw.ElapsedMilliseconds);
			//		sw.Restart();
				}
			//	sw.Stop();

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine("[SONG] " + e.ToString());
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
			// if one track contains a disc number and the other doesn't, the one with the disc number is greater
			//if (x.DiscNumber == 0 && y.DiscNumber != 0) return 1;
			//else if (x.DiscNumber != 0 && y.DiscNumber == 0) return -1;

			if (x.DiscNumber == y.DiscNumber && x.TrackNumber == y.TrackNumber) return 0;

			// if the disc numbers are equal, we have to compare by track
			else if (x.DiscNumber == y.DiscNumber) return x.TrackNumber > y.TrackNumber ? 1 : -1;

			// if the disc numbers are not equal, the one with the higher disc number is greater.
			else return x.DiscNumber > y.DiscNumber ? 1 : -1;
		}
	}
}
