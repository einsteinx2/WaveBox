using System;

namespace WaveBox.Core
{
	public class ArtItem
	{
		public int? ArtId { get; set; }

		public int? ItemId { get; set; }

		public ArtItem()
		{
		}

		public override string ToString()
		{
			return String.Format("[ArtItem: ItemId={0}, ArtId={1}]", this.ItemId, this.ArtId);
		}
	}
}

