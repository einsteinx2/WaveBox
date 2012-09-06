using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.SQLite;
using WaveBox.DataModel.Model;

namespace WaveBox.DataModel.Singletons
{
	static class Database
	{
		public static IDbConnection GetDbConnection()
		{
            string connString = "Data Source=wavebox.db;Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;";
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
		public static void AddNamedParam(this IDbCommand command, string name, Object value)
		{
			IDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			command.Parameters.Add(param);
		}

		public static object GetValueOrNull(this IDataReader reader, int ordinal)
		{
			object value = reader.GetValue(ordinal);
			return value == DBNull.Value ? null : value;
		}

		public static int? GetInt32OrNull(this IDataReader reader, int ordinal)
		{
			int? value = null;
			try
			{
				value = reader.GetInt32(ordinal);
			}
			catch { }

			return value;
		}

		public static long? GetInt64OrNull(this IDataReader reader, int ordinal)
		{
			long? value = null;
			try
			{
				value = reader.GetInt64(ordinal);
			}
			catch { }

			return value;
		}

		public static string GetStringOrNull(this IDataReader reader, int ordinal)
		{
			string value = null;
			try
			{
				value = reader.GetString(ordinal);
			}
			catch { }

			return value;
		}

		/* public bool DeleteItemId (int itemId)
		{
			bool success = false;
			IDbConnection conn = null;
			IDataReader reader = null;
			try 
			{
				conn = Database.GetDbConnection ();
				IDbCommand q = Database.GetDbCommand ("DELETE FROM item WHERE item_id = @itemid", conn);
				q.AddNamedParam("@itemid", itemId);
				q.Prepare ();
				int affected = (int)q.ExecuteNonQuery();

				if (affected >= 1)
					success = true;
			} 
			catch (Exception e) 
			{
				Console.WriteLine ("[DATABASE] ERROR deleting item id: " + e.ToString());
			} 
			finally 
			{
				Database.Close(conn, reader);
			}

			return success;
		}*/
	}
}
