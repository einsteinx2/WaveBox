using System;
using WaveBox.Core.Extensions;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;

namespace WaveBox.Core.Model
{
	public class MusicBrainzCheckDate
	{
		public int? ItemId { get; set; }

		public long? Timestamp { get; set; }

		public MusicBrainzCheckDate(int itemId)
		{
			ItemId = itemId;
			Timestamp = DateTime.UtcNow.ToUniversalUnixTimestamp();
		}

		public MusicBrainzCheckDate()
		{
		}

		public override string ToString()
		{
			return String.Format("[MusicBrainzCheckDate: ItemId={0}, Timestamp={1}]", this.ItemId, this.Timestamp);
		}
	}
}

