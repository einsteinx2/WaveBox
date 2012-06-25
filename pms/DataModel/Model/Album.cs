using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;

namespace pms.DataModel.Model
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
