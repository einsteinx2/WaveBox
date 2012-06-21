using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using Bend.Util;
using System.Threading;
using pms.DataModel.Singletons;

namespace pms
{
	class Program
	{
		static void Main(string[] args)
		{
			var conn = new SqlCeConnection("DataSource = \"pms.sdf\"");
			//conn.Open();
			var query = new SqlCeCommand("insert into artist (artist_name) values('omg')", conn);
			//var result = query.ExecuteNonQuery();

			query = new SqlCeCommand("select * from artist");
			query.Connection = conn;
			SqlCeDataReader result2 = query.ExecuteReader();
			

			//result2.Read();
			//do
			//{
			//    Console.WriteLine("{0} : {1}", result2.GetInt32(0), result2.GetString(1));
			//} while (result2.Read());

			var settings = Settings.Instance;

			var http = new PmsHttpServer(8080);
			http.listen();

			Thread.Sleep(Timeout.Infinite);
		}
	}
}
