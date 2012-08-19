using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.DataModel.Model
{
	public enum ItemType
	{
		Artist = 1, // Starts with 1 for database compatibility
		Album = 2,
		Song = 3,
		Folder = 4,
		Playlist = 5,
		PlaylistItem = 6,
        Podcast = 7,
        PodcastEpisode = 8,
		User = 9,
		Video = 10,
		Bookmark = 11,
		BookmarkItem = 12,
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
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
			return ItemType.Unknown;
		}
	}
}
