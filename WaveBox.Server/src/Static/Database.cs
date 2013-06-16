using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using System.IO;
using WaveBox.Core.Injected;

namespace WaveBox.Static
{
	public class Database : IDatabase
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly string databaseFileName = "wavebox.db";
		public string DatabaseTemplatePath() { return "res" + Path.DirectorySeparatorChar + databaseFileName; }
		public string DatabasePath() { return ServerUtility.RootPath() + databaseFileName; }

		private static readonly string querylogFileName = "wavebox_querylog.db";
		public string QuerylogTemplatePath() { return "res" + Path.DirectorySeparatorChar + querylogFileName; }
		public string QuerylogPath() { return ServerUtility.RootPath() + querylogFileName; }

		private static readonly object dbBackupLock = new object();
		public object DbBackupLock { get { return dbBackupLock; } }

		public Database()
		{
		}

		public void DatabaseSetup()
		{
			if (!File.Exists(DatabasePath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Database file doesn't exist; Creating it : " + databaseFileName);

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
					if (logger.IsInfoEnabled) logger.Info("Query log database file doesn't exist; Creating it : " + querylogFileName);

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
			return new SQLite.SQLiteConnection(DatabasePath());
			//return connFactory.Create(DatabasePath() + ";Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;");
		}

		public ISQLiteConnection GetQueryLogSqliteConnection()
		{
			return new SQLite.SQLiteConnection(QuerylogPath());
			//return connFactory.Create(DatabasePath() + ";Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;");
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
				conn.Close();
			}

			return -1;
		}
	}
}

