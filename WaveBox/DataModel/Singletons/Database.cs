using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using System.Threading;
using System.IO;

namespace WaveBox.DataModel.Singletons
{
	class Database
	{
		private static SqliteConnection dbConn;
		public static readonly Object dbLock = new Object();

		public static SqliteConnection GetDbConnection ()
		{
			lock (dbLock) 
			{
				if (dbConn == null)
				{
					dbConn = new SqliteConnection("Data Source = \"wavebox.db\"");
					/*dbConn = new SqliteConnection();

					string dbFilename = @"wavebox.db";
					string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

					if (File.Exists(dbFilename)) 
					{
						Console.WriteLine("db file exists");
					}

					dbConn.ConnectionString = cs;*/
				}
			
				if (dbConn.State == System.Data.ConnectionState.Closed)
				{
					dbConn.Open();
				}
			}
			return dbConn;
		}

		public static void Close(SqliteConnection c, SqliteDataReader r)
		{
			if (c != null && c.State != System.Data.ConnectionState.Closed)
			{
				c.Close();
			}

			if (r != null && !r.IsClosed)
			{
				r.Close();
			}
		}
	}
}
