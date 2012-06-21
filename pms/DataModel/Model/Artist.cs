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
				return ItemType.ARTIST.ItemTypeId;
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
				string query = string.Format("SELECT * FROM artist LEFT JOIN item_type_art ON item_type_id = {0} AND item_id = artist_id WHERE artist_name = ?", 
					ItemTypeId, artistName);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
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
				_artistId = reader.GetInt32(0);
				_artistName = reader.GetString(1);
				_artId = reader.GetInt32(2);
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
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
				string query = string.Format("INSERT INTO artist (artist_name) VALUES ({0})", artistName);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
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
			List<Album> albums = new List<Album>();

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

			// TODO: Sortin!

			return albums;
		}

		public List<Song> listOfSongs()
		{
			List<Song> albums = new List<Song>();


			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}

			return new List<Song>();
		}

		public static Artist artistForName(string artistName)
		{
			return new Artist();
		}

		public static List<Artist> allArtists()
		{
			return new List<Artist>();
		}

		static int compareAlbumsByName(Album x, Album y)
		{
			if(x.AlbumName == y.AlbumName) return 0;
			if (x.AlbumName == null)
			{
				if (y.AlbumName == null)
				{
					return 0;
				}
			}
		}
	}
}
