using System;

namespace WaveBox.Model
{
	public class PlaylistItem
	{
		public int? PlaylistItemId { get; set; }

		public int? PlaylistId { get; set; }

		public ItemType? ItemType { get; set; }

		public int? ItemId { get; set; }

		public int? ItemPosition { get; set; }

		public PlaylistItem()
		{
		}
	}
}