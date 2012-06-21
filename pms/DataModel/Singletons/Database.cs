using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;

namespace pms.DataModel.Singletons
{
	class Database
	{
		private static Database instance;
		public static Database Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Database();
				}

				return instance;
			}
		}

		private Database()
		{
		}

		public static SqlCeConnection getDbConnection()
		{
			return new SqlCeConnection("DataSource = \"pms.sdf\"");
		}

		public static void close(SqlCeConnection c, SqlCeDataReader r)
		{
			if (!(c.State == System.Data.ConnectionState.Closed))
			{
				c.Close();
			}

			if (!r.IsClosed)
			{
				r.Close();
			}
		}
	}
}
