using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using MediaFerry.DataModel.Singletons;

namespace MediaFerry.DataModel.Model
{
	public class Album
	{
		public int ItemTypeId
		{
			get
			{
				return ItemType.ALBUM.getItemTypeId();
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

		private int _albumId;
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
				var q = new SqlCeCommand("SELECT * FROM album LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND album_id = @albumid");
				q.Parameters.AddWithValue("@itemtypeid", ItemTypeId);
				q.Parameters.AddWithValue("@albumid", albumId);

				Database.dblock.WaitOne();
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
				Database.dblock.ReleaseMutex();
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

				Database.dblock.WaitOne();
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
				Database.dblock.ReleaseMutex();
				Database.close(conn, result);
			}

		}

        public static Album albumForName(string albumName)
        {
            return new Album();
        }

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			_artistId = reader.GetInt32(0);
			_albumId = reader.GetInt32(1);
			_albumName = reader.GetString(2);
			_artId = reader.GetInt32(3);
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}
	}
}
