using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class Album
	{
		[JsonProperty("itemTypeId")]
		public int ItemTypeId
		{
			get
			{
				return ItemType.ALBUM.getItemTypeId();
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

		private int _artId;
		[JsonProperty("artId")]
		public int ArtId
		{
			get
			{
				return _artId;
			}

			set
			{
				_artId = value;
			}

		}

		public Album()
		{
		}

		public Album(SqlCeDataReader reader)
		{
			_setPropertiesFromQueryResult(reader);
		}

		public Album(int albumId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader result = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM album LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND album_id = @albumid WHERE album_id = @albumid");
				q.Parameters.AddWithValue("@itemtypeid", ItemTypeId);
				q.Parameters.AddWithValue("@albumid", albumId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				result = q.ExecuteReader();

				if (result.Read())
				{
					_setPropertiesFromQueryResult(result);
				}

				result.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				Database.close(conn, result);
			}
		}

		public Album(string albumName)
		{
			if (albumName == null || albumName == "")
			{
				return;
			}

			AlbumName = albumName;

			SqlCeConnection conn = null;
			SqlCeDataReader result = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM album LEFT JOIN item_type_art ON item_type_id = @itemtypeid AND item_id = album_id WHERE album_name  = @albumname");
				q.Parameters.AddWithValue("@itemtypeid", ItemTypeId);
				q.Parameters.AddWithValue("@albumname", AlbumName);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				result = q.ExecuteReader();

				if (result.Read())
				{
					_setPropertiesFromQueryResult(result);
				}

				else AlbumName = albumName;


				result.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch(Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				Database.close(conn, result);
			}

		}

		private static bool _insertAlbum(string albumName, int artistId)
		{
			bool success = false;

			SqlCeConnection conn = null;
			SqlCeDataReader result = null;

			try
			{
				var q = new SqlCeCommand("INSERT INTO album (album_name, artist_id) VALUES(@albumname, @artistid)");
				//q.Parameters.AddWithValue("@albumid", DBNull.Value);
				q.Parameters.AddWithValue("@albumname", albumName);
				q.Parameters.AddWithValue("@artistid", artistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				int affected = (int)q.ExecuteNonQuery();

				if(affected >= 1) success = true;
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				Database.close(conn, result);
			}

			return success;
		}

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			if(reader.IsDBNull(reader.GetOrdinal("artist_id")))
			{
				_artistId = 0;
			}
			else _artistId = reader.GetInt32(reader.GetOrdinal("artist_id"));

			if (reader.IsDBNull(reader.GetOrdinal("album_id")))
			{
				_albumId = 0;
			}
			else _albumId = reader.GetInt32(reader.GetOrdinal("album_id"));

			if (reader.IsDBNull(reader.GetOrdinal("album_name")))
			{
				_albumName = "";
			}
			else _albumName = reader.GetString(reader.GetOrdinal("album_name"));

			if (reader.IsDBNull(reader.GetOrdinal("art_id")))
			{
				_artId = 0;
			}
			else _artId = reader.GetInt32(reader.GetOrdinal("art_id"));
		}

		public Artist artist()
		{
			return new Artist(ArtistId);
		}

		// TO DO
		public void autoTag()
		{
		}

		public List<Song> listOfSongs()
		{
			var songs = new List<Song>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT song.*, artist.artist_name, album.album_name, item_type_art.art_id FROM song " +
										 "LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND item_id = song_id " +
										 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
										 "LEFT JOIN album ON song_album_id = album.album_id " +
										 "WHERE song_album_id = @albumid");

				q.Parameters.AddWithValue("@itemtypeid", new Song().ItemTypeId);
				q.Parameters.AddWithValue("@albumid", AlbumId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songs.Add(new Song(reader));
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				Database.close(conn, reader);
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public static Album albumForName(string albumName, int artistId)
		{
			if (albumName == "" || albumName == null || artistId == 0)
			{
				return new Album();
			}

			Album a = new Album(albumName);

			if (a.AlbumId == 0)
			{
				a = null;

				if(_insertAlbum(albumName, artistId))
				{
					a = albumForName(albumName, artistId);
				}
			}

			return a;
		}

		public static List<Album> allAlbums()
		{
			var albums = new List<Album>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM album LEFT JOIN item_type_art ON item_type_id = @itemtypeid AND item_id = album_id");

				q.Parameters.AddWithValue("@itemtypeid", new Album().ItemTypeId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					albums.Add(new Album(reader));
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				Database.close(conn, reader);
			}

			albums.Sort(CompareAlbumsByName);
			return albums;
		}

		public static List<Album> randomAlbums()
		{
			var random = new List<Album>();
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT TOP @count * FROM album LEFT JOIN item_type_art ON item_type_id = @itemtypeid AND item_id = album_id" +
										 "ORDER BY NEWID()");

				q.Parameters.AddWithValue("@itemtypeid", new Album().ItemTypeId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					random.Add(new Album(reader));
				}
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

			return random;
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}
	}
}
