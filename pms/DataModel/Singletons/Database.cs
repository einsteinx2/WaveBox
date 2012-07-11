using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Threading;


namespace WaveBox.DataModel.Singletons
{
	class Database
	{
		private static Database instance;
		public static Mutex dbLock;
		private static SqlCeConnection dbconn;
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

		public static SqlCeConnection getDbConnection()
		{
			if (dbconn == null)
			{
				dbconn = new SqlCeConnection("DataSource = \"pms.sdf\"");
			}

			while ((dbconn.State == System.Data.ConnectionState.Closed))
			{
				dbconn.Open();
			}

			return dbconn;
		}

		public static void close(SqlCeConnection c, SqlCeDataReader r)
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
