using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Community.CsharpSqlite.SQLiteClient;
using Community.CsharpSqlite;
using System.Threading;


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
					//dbConn = new SqliteConnection("Data Source = \"wavebox.db\"");
					dbConn = new SqliteConnection();
					dbConn.ConnectionString = "Version=3,uri=file:wavebox.db";
				}
			
				while ((dbConn.State == System.Data.ConnectionState.Closed))
				{
					dbConn.Open();
				}
			}

			return dbConn;
		}

		public static void Close(SqliteConnection c, SqliteDataReader r)
		{
			if (!(c == null) && !(c.State == System.Data.ConnectionState.Closed))
			{
				//c.Close();
			}

			if (!(r == null) && !r.IsClosed)
			{
				r.Close();
			}
		}
	}
}
