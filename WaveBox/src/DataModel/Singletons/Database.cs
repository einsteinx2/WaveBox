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
		public static void AddNamedParam(this IDbCommand command, string name, Object value)
		{
			IDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			command.Parameters.Add(param);
		}

		public static int? GenerateItemId(ItemType itemType)
		{
			int? itemId = null;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO item (item_type_id) VALUES (@itemType)", conn);
				q.AddNamedParam("@itemType", itemType);
				q.Prepare();
				int affected = (int)q.ExecuteNonQuery();

				if (affected >= 1)
				{
					IDataReader reader2 = null;
					try
					{
						q.CommandText = "SELECT last_insert_rowid()";
						reader2 = q.ExecuteReader();

						if (reader2.Read())
						{
							itemId = reader2.GetInt32(0);
						}
					}
					catch(Exception e)
					{
						Console.WriteLine("[ITEMID] GenerateItemId ERROR: " + e.ToString());
					}
					finally
					{
						Database.Close(null, reader2);
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("[ITEMID] GenerateItemId ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return itemId;
		}

		public static ItemType ItemTypeForItemId(int itemId)
		{
			int itemTypeId = 0;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT item_type_id FROM item WHERE item_id = @itemid", conn);
				q.AddNamedParam("@itemid", itemId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					itemTypeId = reader.GetInt32(0);
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("[ALBUM(3)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return ItemTypeExtensions.ItemTypeForId(itemTypeId);
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
