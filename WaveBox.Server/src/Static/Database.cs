using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Static;

namespace WaveBox.Static
{
	public class Database : IDatabase
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly string DATABASE_FILE_NAME = "wavebox.db";
		public string DatabaseTemplatePath { get { return "res" + Path.DirectorySeparatorChar + DATABASE_FILE_NAME; } }
		public string DatabasePath { get { return ServerUtility.RootPath() + DATABASE_FILE_NAME; } }

		private static readonly string QUERY_LOG_FILE_NAME = "wavebox_querylog.db";
		public string QuerylogTemplatePath { get { return "res" + Path.DirectorySeparatorChar + QUERY_LOG_FILE_NAME; } }
		public string QuerylogPath { get { return ServerUtility.RootPath() + QUERY_LOG_FILE_NAME; } }

		private static readonly object dbBackupLock = new object();
		public object DbBackupLock { get { return dbBackupLock; } }

		// Sqlite connection pool
		private static readonly int MAX_CONNECTIONS = 100;
		private SQLiteConnectionPool mainPool;
		private SQLiteConnectionPool logPool;

		public Database()
		{
			mainPool = new SQLiteConnectionPool(MAX_CONNECTIONS, DatabasePath);
			logPool = new SQLiteConnectionPool(MAX_CONNECTIONS, QuerylogPath);
		}

		public void DatabaseSetup()
		{
			if (!File.Exists(DatabasePath))
			{
				try
				{
					logger.IfInfo("Database file doesn't exist; Creating it : " + DATABASE_FILE_NAME);

					// new filestream on the template
					FileStream dbTemplate = new FileStream(DatabaseTemplatePath, FileMode.Open);

					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];

					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));

					// write it all out
					System.IO.File.WriteAllBytes(DatabasePath, dbData);

					// close the template file
					dbTemplate.Close();
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}

			if (!File.Exists(QuerylogPath))
			{
				try
				{
					logger.IfInfo("Query log database file doesn't exist; Creating it : " + QUERY_LOG_FILE_NAME);

					// new filestream on the template
					FileStream dbTemplate = new FileStream(QuerylogTemplatePath, FileMode.Open);

					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];

					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));

					// write it all out
					System.IO.File.WriteAllBytes(QuerylogPath, dbData);

					// close the template file
					dbTemplate.Close();
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
		}

		public ISQLiteConnection GetSqliteConnection()
		{
			return mainPool.GetSqliteConnection();
		}

		public void CloseSqliteConnection(ISQLiteConnection conn)
		{
			mainPool.CloseSqliteConnection(conn);
		}

		public ISQLiteConnection GetQueryLogSqliteConnection()
		{
			return logPool.GetSqliteConnection();
		}

		public void CloseQueryLogSqliteConnection(ISQLiteConnection conn)
		{
			logPool.CloseSqliteConnection(conn);
		}

		public long LastQueryLogId()
		{
			// Log the query
			ISQLiteConnection conn = null;
			try
			{
				conn = GetQueryLogSqliteConnection();
				return conn.ExecuteScalar<long>("SELECT MAX(QueryId) FROM QueryLog");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				CloseQueryLogSqliteConnection(conn);
			}

			return -1;
		}

		public IList<QueryLog> QueryLogsSinceId(int queryId)
		{
			// Return all queries >= this id
			ISQLiteConnection conn = null;
			try
			{
				// Gather a list of queries from the query log, which can be used to synchronize a local database
				conn = GetQueryLogSqliteConnection();
				return conn.Query<QueryLog>("SELECT * FROM QueryLog WHERE QueryId >= ?", queryId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				// Ensure database closed
				CloseQueryLogSqliteConnection(conn);
			}

			return new List<QueryLog>();
		}
	}
}
