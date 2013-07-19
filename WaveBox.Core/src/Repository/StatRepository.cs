using System;
using WaveBox.Core.Injection;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model.Repository
{
	public class StatRepository : IStatRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public StatRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		// Timestamp is UTC unixtime
		public bool RecordStat(int itemId, StatType statType, long timestamp)
		{
			ISQLiteConnection conn = null;
			bool success = false;
			try
			{
				conn = database.GetSqliteConnection();
				var stat = new Stat();
				stat.StatType = statType;
				stat.ItemId = itemId;
				stat.Timestamp = timestamp;
				int affected = conn.Insert(stat);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}
	}
}

