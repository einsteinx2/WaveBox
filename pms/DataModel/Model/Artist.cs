using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using pms.DataModel.Singletons;

namespace pms.DataModel.Model
{
	public class Artist
	{
		/// <summary>
		/// Properties
		/// </summary>
		/// 
		public int ItemTypeId
		{
			get
			{
				return ItemType.ARTIST.getItemTypeId();
			}
		}

		private int _artistId;
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

		private int _artId;
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

		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{
		}

		public Artist(SqlCeDataReader reader)
		{
			_setPropertiesFromQueryResult(reader);
		}

		public Artist(int artistId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				string query = string.Format("SELECT * FROM artist LEFT JOIN item_type_art ON item_type_art.item_type_id = {0} AND item_id = artist_id WHERE artist_id = {1}",
					ItemTypeId, artistId);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}

				else Console.WriteLine("Artist constructor query returned no results");
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}
		}

		public Artist(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return;
			}

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();


				var q = new SqlCeCommand("SELECT * FROM artist LEFT JOIN item_type_art ON item_type_id = @itemtypeid AND item_id = artist_id WHERE artist_name = @artistname");
				q.Connection = conn;
				q.Parameters.AddWithValue("@itemtypeid", (int)ItemTypeId);
				q.Parameters.AddWithValue("@artistname", artistName);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}

				else _artistName = artistName;
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}
		}

		/// <summary>
		/// Private methods
		/// </summary>

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			try
			{
				_artistId = reader.GetInt32(reader.GetOrdinal("artist_id"));
				_artistName = reader.GetString(reader.GetOrdinal("artist_name"));
				//_artId = reader.GetInt32(reader.GetOrdinal("art_id"));
			}

			catch (SqlCeException e)
			{
				if (e.InnerException.ToString() == "SqlNullValueException") { }
			}
		}

		private static bool _insertArtist(string artistName)
		{
			bool success = false;
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				var q = new SqlCeCommand("INSERT INTO artist (artist_name) VALUES (@artistname)");
				q.Connection = conn;
				q.Parameters.AddWithValue("@artistname", artistName);
				q.Prepare();
				int affected = q.ExecuteNonQuery();

				if (affected == 1)
				{
					success = true;
				}

				else success = false;
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}

			return success;
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public List<Album> listOfAlbums()
		{
			var albums = new List<Album>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();
				string query = string.Format("SELECT * FROM album LEFT JOIN item_type_art ON item_type_id = {0} AND item_id = album_id WHERE artist_id = {1}",
					ItemTypeId, ArtistId);

				var q = new SqlCeCommand(query);
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
				Database.close(conn, reader);
			}

			albums.Sort(Album.CompareAlbumsByName);

			return albums;
		}

		public List<Song> listOfSongs()
		{
			var songs = new List<Song>();


			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				string query = "SELECT song.*, artist.artist_name, album.album_name FROM song " +
					string.Format("LEFT JOIN item_type_art ON item_type_art.item_type_id = {0} AND item_id = song_id ", new Song().ItemTypeId) +
					string.Format("LEFT JOIN artist ON song_artist_id = artist_id ") +
					string.Format("LEFT JOIN album ON song_album_id = album_id ") +
					string.Format("WHERE song_artist_id = {0}", ArtistId);

				var q = new SqlCeCommand(query);
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
				Database.close(conn, reader);
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);

			return songs;
		}

		public static Artist artistForName(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return new Artist();
			}

			// check to see if the artist exists
			var anArtist = new Artist(artistName);

			// if not, create it.
			if (anArtist.ArtistId == 0)
			{
				anArtist = null;
				if (_insertArtist(artistName))
				{
					anArtist = artistForName(artistName);
				}
			}

			// then return the artist object retrieved or created.
			return anArtist;
		}

		public List<Artist> allArtists()
		{
			var artists = new List<Artist>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				string query = string.Format("SELECT * FROM artist LEFT JOIN item_type_art ON item_type_id = {0} AND item_id = artist_id", ItemTypeId);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					artists.Add(new Artist(reader));
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}

			artists.Sort(Artist.CompareArtistsByName);

			return artists;
		}

		public static int CompareArtistsByName(Artist x, Artist y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.ArtistName, y.ArtistName);
		}
	}
}
