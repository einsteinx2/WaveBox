using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using System.IO;
using WaveBox.Core.Injected;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace WaveBox.Static
{
	public class Database : IDatabase
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly string DATABASE_FILE_NAME = "wavebox.db";
		public string DatabaseTemplatePath() { return "res" + Path.DirectorySeparatorChar + DATABASE_FILE_NAME; }
		public string DatabasePath() { return ServerUtility.RootPath() + DATABASE_FILE_NAME; }

		private static readonly string QUERY_LOG_FILE_NAME = "wavebox_querylog.db";
		public string QuerylogTemplatePath() { return "res" + Path.DirectorySeparatorChar + QUERY_LOG_FILE_NAME; }
		public string QuerylogPath() { return ServerUtility.RootPath() + QUERY_LOG_FILE_NAME; }

		private static readonly object dbBackupLock = new object();
		public object DbBackupLock { get { return dbBackupLock; } }

		// Sqlite connection pool
		private static readonly int MAX_CONNECTIONS = 10;
		private SqliteConnectionPool mainPool;
		private SqliteConnectionPool logPool;

		public Database()
		{
			mainPool = new SqliteConnectionPool(MAX_CONNECTIONS, DatabasePath());
			logPool = new SqliteConnectionPool(MAX_CONNECTIONS, QuerylogPath());
		}

		public void DatabaseSetup()
		{
			if (!File.Exists(DatabasePath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Database file doesn't exist; Creating it : " + DATABASE_FILE_NAME);

					// new filestream on the template
					FileStream dbTemplate = new FileStream(DatabaseTemplatePath(), FileMode.Open);

					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];

					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));

					// write it all out
					System.IO.File.WriteAllBytes(DatabasePath(), dbData);

					// close the template file
					dbTemplate.Close();
				} 
				catch (Exception e)
				{
					logger.Error(e);
				}
			}

			if (!File.Exists(QuerylogPath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Query log database file doesn't exist; Creating it : " + QUERY_LOG_FILE_NAME);

					// new filestream on the template
					FileStream dbTemplate = new FileStream(QuerylogTemplatePath(), FileMode.Open);

					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];

					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));

					// write it all out
					System.IO.File.WriteAllBytes(QuerylogPath(), dbData);

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
				CloseSqliteConnection(conn);
			}

			return -1;
		}

		private class SqliteConnectionPool
		{
			private string databasePath = "";
			private int maxConnections = 0;
			private int usedConnections = 0;
			private object connectionPoolLock = new object();
			private Stack<ISQLiteConnection> availableConnections = new Stack<ISQLiteConnection>();

			public SqliteConnectionPool(int max, string path)
			{
				maxConnections = max;
				databasePath = path;
			}

			public ISQLiteConnection GetSqliteConnection()
			{
				lock (connectionPoolLock)
				{
					ISQLiteConnection conn = null;
					if (availableConnections.Count > 0)
					{
						// Grab an existing connection
						conn = availableConnections.Pop();
					}
					else if (usedConnections < maxConnections)
					{
						// There are no available connections, and we have room for more open connections, so make a new one
						conn = new SQLite.SQLiteConnection(databasePath);
					}

					if (!ReferenceEquals(conn, null))
					{
						// We got a connection, so increment the counter
						usedConnections++;
						//logger.Info("Got a connection for " + databasePath + " availableConnections: " + availableConnections.Count + " usedConnections: " + usedConnections);
						return conn;
					}
				}

				//logger.Error("Couldn't get connection for " + databasePath);

				// If no connection available, sleep for 50ms and try again
				Thread.Sleep(50);

				// Recurse to try to get another connection
				return GetSqliteConnection();
			}

			public void CloseSqliteConnection(ISQLiteConnection conn)
			{
				if (ReferenceEquals(conn, null))
					return;

				lock (connectionPoolLock)
				{
					// Make the connection available and decrement the counter
					availableConnections.Push(conn);
					usedConnections--;
					//logger.Info("Closed connection for " + databasePath + " availableConnections: " + availableConnections.Count + " usedConnections: " + usedConnections);
				}
			}
		}
	}
}

