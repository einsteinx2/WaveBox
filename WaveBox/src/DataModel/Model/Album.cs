using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class Album
	{
		[JsonProperty("itemTypeId")]
		public int ItemTypeId { get { return ItemType.Album.ItemTypeId(); } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		public Album()
		{
		}

		public Album(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		public Album(int albumId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE album_id = @albumid", conn);
				q.AddNamedParam("@albumid", albumId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(1)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Album(string albumName)
		{
			if (albumName == null || albumName == "")
			{
				return;
			}

			AlbumName = albumName;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE album_name  = @albumname", conn);
				//q.Parameters.AddWithValue("@itemtypeid", ItemTypeId);
				q.AddNamedParam("@albumname", AlbumName);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
				else
				{
					AlbumName = albumName;
				}

			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(2)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		private static bool InsertAlbum(string albumName, int? artistId)
		{
			int? itemId = Database.GenerateItemId(ItemType.Album);
			if (itemId == null)
				return false;

			bool success = false;

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO album (album_id, album_name, artist_id) VALUES (@albumid, @albumname, @artistid)", conn);
				q.AddNamedParam("@albumid", itemId);
				q.AddNamedParam("@albumname", albumName);
				q.AddNamedParam("@artistid", artistId);
				q.Prepare();
				int affected = (int)q.ExecuteNonQuery();

				if (affected >= 1)
					success = true;
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(3)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return success;
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			if(reader.IsDBNull(reader.GetOrdinal("artist_id")))
			{
				this.ArtistId = null;
			}
			else ArtistId = reader.GetInt32(reader.GetOrdinal("artist_id"));

			if (reader.IsDBNull(reader.GetOrdinal("album_id")))
			{
				this.AlbumId = null;
			}
			else this.AlbumId = reader.GetInt32(reader.GetOrdinal("album_id"));

			if (reader.IsDBNull(reader.GetOrdinal("album_name")))
			{
				this.AlbumName = "";
			}
			else this.AlbumName = reader.GetString(reader.GetOrdinal("album_name"));
		}

		public Artist Artist()
		{
			return new Artist(ArtistId);
		}

		// TO DO
		public void AutoTag()
		{
		}

		public List<Song> ListOfSongs()
		{
			var songs = new List<Song>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
					"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
					"LEFT JOIN album ON song_album_id = album.album_id " +
					"WHERE song_album_id = @albumid", conn);
				q.AddNamedParam("@albumid", AlbumId);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songs.Add(new Song(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(4)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public static Album AlbumForName(string albumName, int? artistId)
		{
			if (albumName == "" || albumName == null || artistId == null)
			{
				return new Album();
			}

			Album a = new Album(albumName);

			if (a.AlbumId == null)
			{
				a = null;

				InsertAlbum(albumName, artistId);
				a = AlbumForName(albumName, artistId);
			}

			return a;
		}

		public static List<Album> AllAlbums()
		{
			var albums = new List<Album>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					albums.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(5)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			albums.Sort(CompareAlbumsByName);
			return albums;
		}

		public static List<Album> RandomAlbums()
		{
			var random = new List<Album>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(*) FROM album ORDER BY NEWID() LIMIT 1", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					random.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(6)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return random;
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}
	}
}
