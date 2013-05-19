using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.Model;
using WaveBox.Static;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WaveBox.Model
{
	public enum StatType
	{
		PLAYED = 0,
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
	}

	public static class Stat
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Timestamp is UTC unixtime
		public static bool RecordStat(int itemId, StatType statType, long timeStamp)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			bool success = false;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO stat (time_stamp, item_id, stat_type) " +
													 "VALUES (@timestamp, @itemid, @stattype)", conn);
				q.AddNamedParam("@timestamp", timeStamp);
				q.AddNamedParam("@itemid", itemId);
				q.AddNamedParam("@stattype", (int)statType);
				q.Prepare();
				
				success = q.ExecuteNonQueryLogged() > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return success;
		}

		public static bool RecordStat(this IItem item, StatType statType, long timeStamp)
		{
			return (object)item.ItemId == null ? false : Stat.RecordStat((int)item.ItemId, statType, timeStamp);
		}
	}
}

