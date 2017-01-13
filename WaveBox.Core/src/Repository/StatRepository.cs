using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.Model.Repository {
    public class StatRepository : IStatRepository {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDatabase database;

        public StatRepository(IDatabase database) {
            if (database == null) {
                throw new ArgumentNullException("database");
            }

            this.database = database;
        }

        // Timestamp is UTC unixtime
        public bool RecordStat(int itemId, StatType statType, long timestamp) {
            var stat = new Stat();
            stat.StatType = statType;
            stat.ItemId = itemId;
            stat.Timestamp = timestamp;

            return this.database.InsertObject<Stat>(stat) > 0;
        }
    }
}
