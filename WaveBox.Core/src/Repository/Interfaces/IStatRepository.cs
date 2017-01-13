using System;

namespace WaveBox.Core.Model.Repository {
    public interface IStatRepository {
        bool RecordStat(int itemId, StatType statType, long timestamp);
    }
}

