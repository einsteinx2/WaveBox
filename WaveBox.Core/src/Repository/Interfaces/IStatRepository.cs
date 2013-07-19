using System;

namespace WaveBox.Model.Repository
{
	public interface IStatRepository
	{
		bool RecordStat(int itemId, StatType statType, long timestamp);
	}
}

