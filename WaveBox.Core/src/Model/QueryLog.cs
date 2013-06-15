using System;
using WaveBox.Static;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class QueryLog
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[PrimaryKey]
		public int? QueryId { get; set; }

		public string QueryString { get; set; }

		public string ValuesString { get; set; }

		public QueryLog()
		{
		}

		public static void LogQuery(string queryString, string valuesString)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Gather a list of queries from the query log, which can be used to synchronize a local database
				conn = Injection.Kernel.Get<IDatabase>().GetQueryLogSqliteConnection();
				var log = new QueryLog();
				log.QueryString = queryString;
				log.ValuesString = valuesString;
				conn.Insert(log);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				// Ensure database closed
				conn.Close();
			}
		}

		public static List<QueryLog> QueryLogsSinceId(int queryId)
		{
			// Return all queries >= this id
			ISQLiteConnection conn = null;
			try
			{
				// Gather a list of queries from the query log, which can be used to synchronize a local database
				conn = Injection.Kernel.Get<IDatabase>().GetQueryLogSqliteConnection();
				return conn.Query<QueryLog>("SELECT * FROM QueryLog WHERE QueryId >= ?", queryId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				// Ensure database closed
				conn.Close();
			}

			return new List<QueryLog>();
		}
	}
}

