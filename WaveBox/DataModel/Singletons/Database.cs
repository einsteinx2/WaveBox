using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Threading;


namespace WaveBox.DataModel.Singletons
{
	class Database
	{
		private static SQLiteConnection dbConn;
		public static readonly Object dbLock = new Object();

		public static SQLiteConnection GetDbConnection ()
		{
			lock (dbLock) 
			{
				if (dbConn == null)
				{
					dbConn = new SQLiteConnection("Data Source = \"wavebox.db\"");
				}
			
				while ((dbConn.State == System.Data.ConnectionState.Closed))
				{
					dbConn.Open();
				}
			}

			return dbConn;
		}

		public static void Close(SQLiteConnection c, SQLiteDataReader r)
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
