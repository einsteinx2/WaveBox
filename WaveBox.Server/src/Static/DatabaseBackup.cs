using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SQLite;
using WaveBox.Core;
using WaveBox.Static;

namespace WaveBox {
    public static class DatabaseBackup {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(DatabaseBackup));

        public static string BackupFileName(long queryId) { return "wavebox_backup_" + queryId + ".db"; }
        public static string BackupPath(long queryId) { return ServerUtility.RootPath() + BackupFileName(queryId); }

        [DllImport("e_sqlite3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_backup_init(IntPtr destDb, byte[] destname, IntPtr srcDB, byte[] srcname);

        [DllImport("e_sqlite3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_backup_step(IntPtr backup, int pages);

        [DllImport("e_sqlite3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_backup_finish(IntPtr backup);

        public static string Backup(out long lastQueryId) {
            lock (Injection.Get<IDatabase>().DbBackupLock) {
                lastQueryId = Injection.Get<IDatabase>().LastQueryLogId;
                string fileName = BackupFileName(lastQueryId);

                // If the database is already backed up at this point, return it
                if (File.Exists(BackupPath(lastQueryId))) {
                    return fileName;
                }

                // If not, do the backup then return it
                if (Backup(Injection.Get<IDatabase>().DatabasePath, BackupPath(lastQueryId))) {
                    return fileName;
                }

                // Something failed so return null
                lastQueryId = -1;
                return null;
            }
        }

        public static bool Backup(string sourcePath, string destinationPath) {
            SQLiteConnection source = null;
            SQLiteConnection destination = null;
            try {
                source = new SQLiteConnection(sourcePath);
                destination = new SQLiteConnection(destinationPath);

                byte[] main = Encoding.UTF8.GetBytes("main\0");
                IntPtr backupHandle = sqlite3_backup_init(destination.Handle, main, source.Handle, main);
                if (backupHandle == IntPtr.Zero) {
                    return false;
                }

                int stepResult = sqlite3_backup_step(backupHandle, -1);
                int finishResult = sqlite3_backup_finish(backupHandle);
                if (stepResult != 101 /* SQLITE_DONE */ || finishResult != 0 /* SQLITE_OK */) {
                    logger.Error("Database backup failed (step: " + stepResult + ", finish: " + finishResult + ")");
                    return false;
                }

                // Strip user-sensitive tables from the backup
                string[] tablesToDelete = { "User", "Session", "Server" };
                foreach (string tableName in tablesToDelete) {
                    try {
                        destination.Execute("DROP TABLE IF EXISTS " + tableName);
                    } catch (Exception e) {
                        logger.Error("Error deleting user table in backup: " + e);
                    }
                }

                return true;
            } catch (Exception e) {
                logger.Error("Error backing up database: " + e);
                return false;
            } finally {
                if (source != null) {
                    source.Close();
                }
                if (destination != null) {
                    destination.Close();
                }
            }
        }
    }
}
