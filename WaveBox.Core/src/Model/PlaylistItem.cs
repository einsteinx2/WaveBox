using System;

namespace WaveBox.Core.Model
{
	public class PlaylistItem
	{
		public int? PlaylistItemId { get; set; }

		public int? PlaylistId { get; set; }

		public ItemType ItemType { get; set; }

		public int? ItemId { get; set; }

		public int? ItemPosition { get; set; }

		public PlaylistItem()
		{
		}

		public override string ToString()
		{
			return String.Format("[PlaylistItem: ItemId={0}, PlaylistItemId={1}, PlaylistId={2}]", this.ItemId, this.PlaylistItemId, this.PlaylistItemId, this.PlaylistId);
		}
	}
}
