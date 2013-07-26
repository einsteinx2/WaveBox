using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using System.Threading;

namespace WaveBox.Core
{
	public class SQLiteConnectionPool
	{
		private string databasePath = "";
		private int maxConnections = 0;
		private int usedConnections = 0;
		private object connectionPoolLock = new object();
		private Stack<ISQLiteConnection> availableConnections = new Stack<ISQLiteConnection>();

		public SQLiteConnectionPool(int max, string path)
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
					conn.Execute("PRAGMA synchronous = OFF");
					// Five second busy timeout
					conn.BusyTimeout = new TimeSpan(0, 0, 5); 
				}

				if (!ReferenceEquals(conn, null))
				{
					// We got a connection, so increment the counter
					usedConnections++;
					return conn;
				}
			}

			// If no connection available, sleep for 50ms and try again
			Thread.Sleep(50);

			// Recurse to try to get another connection
			return GetSqliteConnection();
		}

		public void CloseSqliteConnection(ISQLiteConnection conn)
		{
			if (ReferenceEquals(conn, null))
			{
				return;
			}

			lock (connectionPoolLock)
			{
				// Make the connection available and decrement the counter
				availableConnections.Push(conn);
				usedConnections--;
			}
		}
	}
}

