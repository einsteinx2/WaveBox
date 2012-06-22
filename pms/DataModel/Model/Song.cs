using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;

namespace pms.DataModel.Model
{
	public class Song
	{
		public Song()
		{
		}

		public Song(SqlCeDataReader reader)
		{
		}

		public int ItemTypeId
		{
			get
			{
				return ItemType.SONG.getItemTypeId();
			}
		}
		// stub!
		public static int CompareSongsByDiscAndTrack(Song x, Song y)
		{
			return 1;
			//return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}
	}
}
