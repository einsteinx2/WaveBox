using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.SQLite;

namespace WaveBox.DataModel.Singletons
{
	static class Database
	{
		public static IDbConnection GetDbConnection()
		{
			string connString = "Data Source=wavebox.db;Version=3;Pooling=True;Max Pool Size=100;";
			IDbConnection conn = new SQLiteConnection(connString);

			while (conn.State == System.Data.ConnectionState.Closed)
			{
				conn.Open();
			}

			return conn;
		}

		public static IDbCommand GetDbCommand(string queryString, IDbConnection connection)
		{
			return new SQLiteCommand(queryString, (SQLiteConnection)connection);
		}

		public static void Close(IDbConnection connection, IDataReader reader)
		{
			if ((object)reader != null && !reader.IsClosed) 
			{
				reader.Close();
			}

			if ((object)connection != null)
			{
				connection.Close();
			}
		}
	
		// IDbCommand extension to add named parameters without writing a bunch of code each time
		public static void AddNamedParam(this IDbCommand command, string name, object value)
		{
		    IDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			command.Parameters.Add(param);
		}
	}
}
