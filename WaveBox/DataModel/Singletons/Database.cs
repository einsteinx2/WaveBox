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
		private static Database instance;
		public static Mutex dbLock;
		private static SQLiteConnection dbconn;
		public static Database Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Database();
					dbLock = new Mutex();
				}

				return instance;
			}
		}

		private Database()
		{
		}

		public static SQLiteConnection getDbConnection()
		{
			if (dbconn == null)
			{
				dbconn = new SQLiteConnection("DataSource = \"wavebox.db\"");
			}

			while ((dbconn.State == System.Data.ConnectionState.Closed))
			{
				dbconn.Open();
			}

			return dbconn;
		}

		public static void close(SQLiteConnection c, SQLiteDataReader r)
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
