using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;

namespace pms.DataModel.Model
{
	public class Song : MediaItem
	{
		public int ItemTypeId
		{
			get
			{
				return ItemType.SONG.getItemTypeId();
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

		private string _songName;
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

		public Song()
		{
		}

		public Song(int songId)
		{
		}

		public Song(SqlCeDataReader reader)
		{
		}


		// stub!
		public static int CompareSongsByDiscAndTrack(Song x, Song y)
		{
			return 1;
			//return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}
	}
}
