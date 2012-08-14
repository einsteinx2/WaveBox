using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.DataModel.Model
{
	public enum ItemType
	{
		ARTIST = 1,
		ALBUM = 2,
		SONG = 3,
		VIDEO = 4,
        PODCAST = 5,
        PODCASTEPISODE = 6,
		UNKNOWN = -1
	}

	public static class ItemTypeExtensions
	{
		public static int ItemTypeId(this ItemType val)
		{
			return (int)val;
		}

		public static ItemType ItemTypeForId(int id)
		{
			// check the id number against all the enum types
			foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
			{
				if ((int)type == id)
				{
					return type;
				}
			}

			// if there's no match, return unknown.
			return ItemType.UNKNOWN;
		}
	}
}
