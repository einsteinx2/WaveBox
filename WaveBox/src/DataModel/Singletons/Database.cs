using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.SQLite;
using WaveBox.DataModel.Model;
using System.Runtime.InteropServices;
using System.Reflection;

namespace WaveBox.DataModel.Singletons
{
	static class Database
	{
		public static IDbConnection GetDbConnection()
		{
			return GetDbConnection("wavebox.db");
		}

		public static IDbConnection GetDbConnection(string dbName)
		{
			if ((object)dbName == null)
				dbName = "wavebox.db";

			string connString = "Data Source=" + dbName + ";Version=3;Pooling=True;Max Pool Size=100;synchronous=OFF;";
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
		//
		public static void AddNamedParam(this IDbCommand command, string name, Object value)
		{
			IDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value == null ? DBNull.Value : value;
			command.Parameters.Add(param);
		}

		// IDataReader extension to safely get null values
		//
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
	
		// Backup SQLite database (modified code from: http://sqlite.phxsoftware.com/forums/t/2403.aspx)
		//

		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr sqlite3_backup_init(IntPtr destDb, byte[] destname, IntPtr srcDB, byte[] srcname);
		                                                                                                    
		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_step(IntPtr backup, int pages);

		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_remaining(IntPtr backup);

		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_pagecount(IntPtr backup);

		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_sleep(int milliseconds);
		                                                                                                    
		[DllImport("System.Data.SQLite.DLL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_finish(IntPtr backup);
		                                                                                                    
		public static bool Backup(SQLiteConnection source, SQLiteConnection destination)
		{
			IntPtr sourceHandle = GetConnectionHandle(source);
			IntPtr destinationHandle = GetConnectionHandle(destination);
			
			IntPtr backupHandle = sqlite3_backup_init(destinationHandle, SQLiteConvert.ToUTF8("main"), sourceHandle, SQLiteConvert.ToUTF8("main"));
			if (backupHandle != IntPtr.Zero)
			{
				sqlite3_backup_step(backupHandle, -1);
				sqlite3_backup_finish(backupHandle);
				return true;
			}
			return false;
		}

		// SQLITE_OK = 0
		// SQLITE_BUSY = 5
		// SQLITE_LOCKED = 6
		public static void BackupLive(SQLiteConnection source, SQLiteConnection destination)
		{
			IntPtr sourceHandle = GetConnectionHandle(source);
			IntPtr destinationHandle = GetConnectionHandle(destination);
			
			IntPtr backupHandle = sqlite3_backup_init(destinationHandle, SQLiteConvert.ToUTF8("main"), sourceHandle, SQLiteConvert.ToUTF8("main"));
			if (backupHandle != IntPtr.Zero)
			{
				/* Each iteration of this loop copies 5 database pages from database
      			** pDb to the backup database. If the return value of backup_step()
      			** indicates that there are still further pages to copy, sleep for
      			** 250 ms before repeating. */
				int rc = 0;
				do 
				{
					rc = sqlite3_backup_step(backupHandle, 5);
					//xProgress(sqlite3_backup_remaining(backupHandle), sqlite3_backup_pagecount(backupHandle));
					if(rc == 0 || rc == 5 || rc == 6)
					{
						sqlite3_sleep(250);
					}
				} 
				while(rc == 0 || rc == 5 || rc == 6);
				
				/* Release resources allocated by backup_init(). */
				rc = sqlite3_backup_finish(backupHandle);
			}
		}
		                                                                                                    
        private static IntPtr GetConnectionHandle(SQLiteConnection source)
        {
			object sqlLite3 = GetPrivateFieldValue(source, "_sql");
			object connectionHandle = GetPrivateFieldValue(sqlLite3, "_sql");
			IntPtr handle = (IntPtr)GetPrivateFieldValue(connectionHandle, "handle");
			
			return handle;
        }		
		                                                                                                    
		private static object GetPrivateFieldValue(object instance, string fieldName)
		{
			var filedType = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			
			object result = filedType.GetValue(instance);
			return result;
        }
	}
}
