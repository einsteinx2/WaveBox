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
		[JsonIgnore]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId")]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get { return Art.ArtIdForItemId(AlbumId); } }

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
				Console.WriteLine("[ALBUM(1)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Album(string albumName, int? artistId)
		{
			if (albumName == null || albumName == "")
			{
				return;
			}

			AlbumName = albumName;
			ArtistId = artistId;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE album_name = @albumname AND artist_id = @artistid", conn);
				q.AddNamedParam("@albumname", AlbumName);
				q.AddNamedParam("@artistid", ArtistId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
					//Console.WriteLine("Album constructor, reader.Read = true, AlbumId = " + AlbumId);
				}
				else
				{
					//Console.WriteLine("Album constructor, reader.Read = false, AlbumId = " + AlbumId);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(2)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		private static bool InsertAlbum(string albumName, int? artistId)
		{
			int? itemId = Item.GenerateItemId(ItemType.Album);
			if (itemId == null)
				return false;

			bool success = false;

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO album (album_id, album_name, artist_id) VALUES (@albumid, @albumname, @artistid)", conn);
				q.AddNamedParam("@albumid", itemId);
				q.AddNamedParam("@albumname", albumName);
				q.AddNamedParam("@artistid", artistId);
				q.Prepare();

				success = (q.ExecuteNonQuery() > 0);
			}
			catch (Exception e)
			{
				Console.WriteLine("[ALBUM(3)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return success;
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
        {
            ArtistId = reader.GetInt32OrNull(reader.GetOrdinal("artist_id"));
			AlbumId = reader.GetInt32OrNull(reader.GetOrdinal("album_id"));
			AlbumName = reader.GetStringOrNull(reader.GetOrdinal("album_name"));
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
			List<Song> songs = new List<Song>();

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
				Console.WriteLine("[ALBUM(4)] ERROR: " + e);
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

			Album a = new Album(albumName, artistId);

			if (a.AlbumId == null)
			{
				a = null;
				if (InsertAlbum(albumName, artistId))
				{
					a = AlbumForName(albumName, artistId);
				}
				else
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the album id, it will exist this time
					a = new Album(albumName, artistId);
				}
			}

			return a;
		}

		public static List<Album> AllAlbums()
		{
			List<Album> albums = new List<Album>();
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
				Console.WriteLine("[ALBUM(5)] ERROR: " + e);
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
			List<Album> random = new List<Album>();
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
				Console.WriteLine("[ALBUM(6)] ERROR: " + e);
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
