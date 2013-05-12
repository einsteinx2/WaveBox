using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Threading;
using System.Data.SQLite;
using WaveBox.Model;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;

namespace WaveBox.Singletons
{
	static class Database
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static string databaseFileName = "wavebox.db";
		public static string DatabaseTemplatePath() { return "res" + Path.DirectorySeparatorChar + databaseFileName; }
		public static string DatabasePath() { return WaveBoxMain.RootPath() + databaseFileName; }

		public static string querylogFileName = "wavebox_querylog.db";
		public static string QuerylogTemplatePath() { return "res" + Path.DirectorySeparatorChar + querylogFileName; }
		public static string QuerylogPath() { return WaveBoxMain.RootPath() + querylogFileName; }
		
		public static string BackupFileName(long queryId) { return "wavebox_backup_" + queryId + ".db"; }
		public static string BackupPath(long queryId) { return WaveBoxMain.RootPath() + BackupFileName(queryId); }

		public static void DatabaseSetup()
		{
			if (!File.Exists(DatabasePath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Database file doesn't exist; Creating it. (wavebox.db)");
					
					// new filestream on the template
					FileStream dbTemplate = new FileStream(DatabaseTemplatePath(), FileMode.Open);
					
					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];
					
					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));
					
					// write it all out
					System.IO.File.WriteAllBytes(DatabasePath(), dbData);
					
					// close the template file
					dbTemplate.Close();
				} 
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
			
			if (!File.Exists(QuerylogPath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Query log database file doesn't exist; Creating it. (wavebox_querylog.db)");
					
					// new filestream on the template
					FileStream dbTemplate = new FileStream(QuerylogTemplatePath(), FileMode.Open);
					
					// a new byte array
					byte[] dbData = new byte[dbTemplate.Length];
					
					// read the template file into memory
					dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));
					
					// write it all out
					System.IO.File.WriteAllBytes(QuerylogPath(), dbData);
					
					// close the template file
					dbTemplate.Close();
				} 
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
		}

		public static IDbConnection GetDbConnection()
		{
			return GetDbConnection(DatabasePath());
		}

		public static IDbConnection GetQueryLogDbConnection()
		{
			return GetDbConnection(QuerylogPath());
		}

		public static IDbConnection GetBackupDbConnection(long queryId)
		{
			return GetDbConnection(BackupPath(queryId));
		}

		public static IDbConnection GetDbConnection(string dbName)
		{
			if ((object)dbName == null)
			{
				return null;
			}

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

		//private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public static int ExecuteNonQueryLogged(this IDbCommand command)
		{
			lock (dbBackupLock)
			{
				int result = command.ExecuteNonQuery();

				if (result > 0)
				{
					// Only log successful queries
					IDataParameterCollection parameters = command.Parameters;
					List<object> values = new List<object>();

					// Format the query for saving
					string query = command.CommandText;
					foreach (IDbDataParameter dbParam in parameters)
					{
						// Replace parameter names with ?'s
						query = query.Replace(dbParam.ParameterName, "?");

						// Add the values to the array
						values.Add(dbParam.Value);
					}

					// Log the query
					IDbConnection conn = null;
					try
					{
						conn = Database.GetQueryLogDbConnection();
						IDbCommand q = Database.GetDbCommand("INSERT INTO query_log (query_string, values_string) " +
							"VALUES (@querystring, @valuesstring)", conn);
						q.AddNamedParam("@querystring", query);
						q.AddNamedParam("@valuesstring", JsonConvert.SerializeObject(values, Formatting.None));

						q.Prepare();
						q.ExecuteNonQuery();
					}
					catch(Exception e)
					{
						logger.Error(e);
					}
					finally
					{
						Database.Close(conn, null);
					}
				}

				return result;
			}
		}

		public static long LastQueryLogId()
		{
			// Log the query
			IDbConnection conn = null;
			long queryId = -1;
			try
			{
				conn = Database.GetQueryLogDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT max(query_id) FROM query_log", conn);

				q.Prepare();
				queryId = (long)q.ExecuteScalar();
			}
			catch (Exception e)
			{
				logger.Error(e.ToString());
			}
			finally
			{
				Database.Close(conn, null);
			}

			return queryId;
		}

		// IDbCommand extension to add named parameters without writing a bunch of code each time
		public static void AddNamedParam(this IDbCommand command, string name, Object value)
		{
			IDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value == null ? DBNull.Value : value;
			command.Parameters.Add(param);
		}

		// IDataReader extension to safely get null values
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
				int affected = (int)q.ExecuteNonQueryLogged();

				if (affected >= 1)
					success = true;
			} 
			catch (Exception e) 
			{
				logger.Info ("[DATABASE] ERROR deleting item id: " + e);
			} 
			finally 
			{
				Database.Close(conn, reader);
			}

			return success;
		}*/
	
		// Backup SQLite database (modified code from: http://sqlite.phxsoftware.com/forums/t/2403.aspx)
		//

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr sqlite3_backup_init(IntPtr destDb, byte[] destname, IntPtr srcDB, byte[] srcname);
																											
		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_step(IntPtr backup, int pages);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_remaining(IntPtr backup);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_pagecount(IntPtr backup);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_sleep(int milliseconds);
																											
		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int sqlite3_backup_finish(IntPtr backup);

		private static readonly object dbBackupLock = new object();
		public static string Backup(out long lastQueryId)
		{
			lock (dbBackupLock)
			{
				lastQueryId = LastQueryLogId();
				string fileName = BackupFileName(lastQueryId);

				// If the database is already backed up at this point, return it
				if (File.Exists(fileName))
				{
					return fileName;
				}

				// If not, do the backup then return it
				bool success = Backup((SQLiteConnection)GetDbConnection(), (SQLiteConnection)GetBackupDbConnection(lastQueryId));
				if (success)
				{
					return fileName;
				}

				// Something failed so return null
				lastQueryId = -1;
				return null;
			}
		}

		/*public static bool BackupLive()
		{
			return BackupLive((SQLiteConnection)GetDbConnection(), (SQLiteConnection)GetBackupDbConnection());
		}*/
			   
		public static bool Backup(SQLiteConnection source, SQLiteConnection destination)
		{
			lock (dbBackupLock)
			{
				try
				{
					IntPtr sourceHandle = GetConnectionHandle(source);
					IntPtr destinationHandle = GetConnectionHandle(destination);

					IntPtr backupHandle = sqlite3_backup_init(destinationHandle, SQLiteConvert.ToUTF8("main"), sourceHandle, SQLiteConvert.ToUTF8("main"));
					if (backupHandle != IntPtr.Zero)
					{
						sqlite3_backup_step(backupHandle, -1);
						sqlite3_backup_finish(backupHandle);

						string[] tablesToDelete = { "user", "session", "server" };

						foreach (string tableName in tablesToDelete)
						{
							try
							{
								IDbCommand q = Database.GetDbCommand("DROP TABLE IF EXISTS " + tableName, destination);
								q.Prepare();
								q.ExecuteNonQuery();
							}
							catch(Exception e)
							{
								if (logger.IsInfoEnabled) logger.Info("Error deleting user table in backup: " + e);
							}
						}
						return true;
					}
					return false;
				}
				catch (Exception e)
				{
					if (logger.IsInfoEnabled) logger.Info("Error backup up database: " + e);
				}
				finally
				{
					Database.Close(source, null);
					Database.Close(destination, null);
				}
				return false;
			}
		}

		// SQLITE_OK = 0
		// SQLITE_BUSY = 5
		// SQLITE_LOCKED = 6
		/*public static bool BackupLive(SQLiteConnection source, SQLiteConnection destination)
		{
			lock(dbBackupLock)
			{
				try
				{
					IntPtr sourceHandle = GetConnectionHandle(source);
					IntPtr destinationHandle = GetConnectionHandle(destination);
					
					IntPtr backupHandle = sqlite3_backup_init(destinationHandle, SQLiteConvert.ToUTF8("main"), sourceHandle, SQLiteConvert.ToUTF8("main"));
					if (backupHandle != IntPtr.Zero)
					{
						// Each iteration of this loop copies 5 database pages from database
						// pDb to the backup database. If the return value of backup_step()
						// indicates that there are still further pages to copy, sleep for
						// 250 ms before repeating. 
						int rc = 0;
						do
						{
							rc = sqlite3_backup_step(backupHandle, 10);
							//xProgress(sqlite3_backup_remaining(backupHandle), sqlite3_backup_pagecount(backupHandle));
							if (rc == 0 || rc == 5 || rc == 6)
							{
								sqlite3_sleep(50);
							}
						}
						while(rc == 0 || rc == 5 || rc == 6);
						
						// Release resources allocated by backup_init().
						rc = sqlite3_backup_finish(backupHandle);

						// Delete the user and session tables
						try
						{
							IDbCommand q = Database.GetDbCommand("DROP TABLE IF EXISTS user", destination);
							q.Prepare();
							q.ExecuteNonQuery();
						}
						catch(Exception e)
						{
							if (logger.IsInfoEnabled) logger.Info("Error deleting user table in backup: " + e);
						}
						
						try
						{
							IDbCommand q = Database.GetDbCommand("DROP TABLE IF EXISTS session", destination);
							q.Prepare();
							q.ExecuteNonQuery();
						}
						catch(Exception e)
						{
							if (logger.IsInfoEnabled) logger.Info("Error deleting session table in backup: " + e);
						}

						return true;
					}
					return false;
				}
				catch(Exception e)
				{
					if (logger.IsInfoEnabled) logger.Info("Error backup up database: " + e);
				}
				finally
				{
					Database.Close(source, null);
					Database.Close(destination, null);
				}
				return false;
			}
		}*/

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
