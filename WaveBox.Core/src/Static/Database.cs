using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Threading;
using WaveBox.Model;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;
using SQLite;
using Cirrious.MvvmCross.Plugins.Sqlite;
//using Cirrious.MvvmCross.Platform;

namespace WaveBox.Static
{
	public static class Database
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static string databaseFileName = "wavebox.db";
		public static string DatabaseTemplatePath() { return "res" + Path.DirectorySeparatorChar + databaseFileName; }
		public static string DatabasePath() { return Utility.RootPath() + databaseFileName; }

		public static string querylogFileName = "wavebox_querylog.db";
		public static string QuerylogTemplatePath() { return "res" + Path.DirectorySeparatorChar + querylogFileName; }
		public static string QuerylogPath() { return Utility.RootPath() + querylogFileName; }

		public static readonly object dbBackupLock = new object();

		public static void DatabaseSetup()
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

		public static ISQLiteConnection GetSqliteConnection()
		{
			return new SQLite.SQLiteConnection(DatabasePath());
			//return connFactory.Create(DatabasePath() + ";Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;");
		}

		public static ISQLiteConnection GetQueryLogSqliteConnection()
		{
			return new SQLite.SQLiteConnection(QuerylogPath());
			//return connFactory.Create(DatabasePath() + ";Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;");
		}

		public static long LastQueryLogId()
		{
			// Log the query
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetQueryLogSqliteConnection();
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
