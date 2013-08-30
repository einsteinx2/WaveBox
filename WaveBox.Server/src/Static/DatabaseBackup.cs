using System;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Ninject;
using WaveBox.Static;
using System.Collections.Generic;
using WaveBox.Core.Model;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;

namespace WaveBox
{
	public static class DatabaseBackup
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Backup SQLite database (modified code from: http://sqlite.phxsoftware.com/forums/t/2403.aspx)

		public static string BackupFileName(long queryId) { return "wavebox_backup_" + queryId + ".db"; }
		public static string BackupPath(long queryId) { return ServerUtility.RootPath() + BackupFileName(queryId); }

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
			IDbConnection conn = new System.Data.SQLite.SQLiteConnection(connString);

			while (conn.State == System.Data.ConnectionState.Closed)
			{
				conn.Open();
			}

			return conn;
		}

		public static IDbCommand GetDbCommand(string queryString, IDbConnection connection)
		{
			return new System.Data.SQLite.SQLiteCommand(queryString, (System.Data.SQLite.SQLiteConnection)connection);
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

		public static string Backup(out long lastQueryId)
		{
			lock (Injection.Kernel.Get<IDatabase>().DbBackupLock)
			{
				lastQueryId = Injection.Kernel.Get<IDatabase>().LastQueryLogId;
				string fileName = BackupFileName(lastQueryId);

				// If the database is already backed up at this point, return it
				if (File.Exists(fileName))
				{
					return fileName;
				}

				// If not, do the backup then return it
				bool success = Backup((System.Data.SQLite.SQLiteConnection)GetDbConnection(Injection.Kernel.Get<IDatabase>().DatabasePath), (System.Data.SQLite.SQLiteConnection)GetBackupDbConnection(lastQueryId));
				if (success)
				{
					return fileName;
				}

				// Something failed so return null
				lastQueryId = -1;
				return null;
			}
		}

		public static bool Backup(SQLiteConnection source, SQLiteConnection destination)
		{
			lock (Injection.Kernel.Get<IDatabase>().DbBackupLock)
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

						string[] tablesToDelete = { "User", "Session", "Server" };

						foreach (string tableName in tablesToDelete)
						{
							try
							{
								IDbCommand q = GetDbCommand("DROP TABLE IF EXISTS " + tableName, destination);
								q.Prepare();
								q.ExecuteNonQuery();
							}
							catch (Exception e)
							{
								logger.Error("Error deleting user table in backup: " + e);
							}
						}

						return true;
					}

					return false;
				}
				catch (Exception e)
				{
					logger.Error("Error backup up database: " + e);
				}
				finally
				{
					Close(source, null);
					Close(destination, null);
				}
				return false;
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
