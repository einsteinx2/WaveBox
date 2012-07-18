using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
//using System.Data.H2;
//using Mono.Data.Sqlite;
using System.Data.SQLite;

namespace WaveBox.DataModel.Singletons
{
	static class Database
	{
		public static readonly Object dbLock = new Object();

		//public static int count = 0;

		public static IDbConnection GetDbConnection ()
		{
			//lock (dbLock)
			{
				//IDbConnection conn = new H2Connection("jdbc:h2:wavebox", "pms", "pms");

				//string connString = "Data Source = \"wavebox.db\"";
				//string connString = "Version=3,pooling=true,URI=file:wavebox.db";
				//string connString = "Version=3,pooling=true,URI=file:wavebox.db";
				string connString = "Data Source=wavebox.db;Version=3;Pooling=True;Max Pool Size=100;";
				IDbConnection conn = new SQLiteConnection(connString);

				while (conn.State == System.Data.ConnectionState.Closed)
				{
					conn.Open();
				}

				//count++;
				//Console.WriteLine("getting connection   " + count);
				return conn;
			}
		}

		public static IDbCommand GetDbCommand(string queryString, IDbConnection connection)
		{
			//return new H2Command(queryString, (H2Connection)connection);
			return new SQLiteCommand(queryString, (SQLiteConnection)connection);
		}

		public static void Close (IDbConnection c, IDataReader r)
		{
			if (!(r == null) && !r.IsClosed) 
			{
				r.Close();
			}

			if (!(c == null))// && !(c.State == System.Data.ConnectionState.Closed)) 
			{
				//count--;
				//Console.WriteLine ("Closing connection  " + count);
				c.Close();
			}
		}
	
		// IDbCommand extension to add named parameters without writing a bunch of code each time
		public static void AddNamedParam(this IDbCommand dbCmd, string name, object value)
		{
		    IDataParameter param = dbCmd.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			dbCmd.Parameters.Add(param);
		}
	}
}
