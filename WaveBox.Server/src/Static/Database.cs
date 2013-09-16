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
		public string DatabaseTemplatePath { get { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + DATABASE_FILE_NAME; } }
		public string DatabasePath { get { return ServerUtility.RootPath() + DATABASE_FILE_NAME; } }

		private static readonly string QUERY_LOG_FILE_NAME = "wavebox_querylog.db";
		public string QuerylogTemplatePath { get { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + QUERY_LOG_FILE_NAME; } }
		public string QuerylogPath { get { return ServerUtility.RootPath() + QUERY_LOG_FILE_NAME; } }

		private static readonly object dbBackupLock = new object();
		public object DbBackupLock { get { return dbBackupLock; } }

		private bool isPoolingEnabled = true;

		public int Version 
		{ 
			get 
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = GetSqliteConnection();
					return conn.ExecuteScalar<int>("SELECT VersionNumber FROM Version LIMIT 1");
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					CloseSqliteConnection(conn);
				}

				return 0;
			} 
		}

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
			if (isPoolingEnabled)
			{
				return mainPool.GetSqliteConnection();
			}
			else
			{
				ISQLiteConnection conn = new SQLite.SQLiteConnection(DatabasePath);
				conn.Execute("PRAGMA synchronous = OFF");
				// Five second busy timeout
				conn.BusyTimeout = new TimeSpan(0, 0, 5); 
				return conn;
			}
		}

		public void CloseSqliteConnection(ISQLiteConnection conn)
		{
			if (isPoolingEnabled)
			{
				mainPool.CloseSqliteConnection(conn);
			}
			else
			{
				conn.Close();
			}
		}

		public ISQLiteConnection GetQueryLogSqliteConnection()
		{
			if (isPoolingEnabled)
			{
				return logPool.GetSqliteConnection();
			}
			else
			{
				ISQLiteConnection conn = new SQLite.SQLiteConnection(QuerylogPath);
				conn.Execute("PRAGMA synchronous = OFF");
				// Five second busy timeout
				conn.BusyTimeout = new TimeSpan(0, 0, 5);
				return conn;
			}
		}

		public void CloseQueryLogSqliteConnection(ISQLiteConnection conn)
		{
			if (isPoolingEnabled)
			{
				logPool.CloseSqliteConnection(conn);
			}
			else
			{
				conn.Close();
			}
		}

		public long LastQueryLogId
		{
			get
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
