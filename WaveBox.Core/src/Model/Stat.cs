using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using TagLib;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Static;

namespace WaveBox.Model
{
	public enum StatType
	{
		PLAYED = 0,
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
	}

	public class Stat
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[PrimaryKey]
		public int? StatId { get; set; }

		public StatType? StatType { get; set; }

		public int? ItemId { get; set; }

		public long? Timestamp { get; set; }

		// Timestamp is UTC unixtime
		public static bool RecordStat(int itemId, StatType statType, long timestamp)
		{
			ISQLiteConnection conn = null;
			bool success = false;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
			
			return success;
		}
	}
}

