using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Static;

namespace WaveBox.Static {
    public class Database : IDatabase {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        private static readonly string DATABASE_FILE_NAME = "wavebox.db";
        private static readonly string DATABASE_SCHEMA_FILE_NAME = "wavebox.sql";
        public string DatabaseSchemaPath { get { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + DATABASE_SCHEMA_FILE_NAME; } }
        public string DatabasePath { get { return ServerUtility.RootPath() + DATABASE_FILE_NAME; } }

        private static readonly string MIGRATIONS_DIR_NAME = "migrations";
        public string MigrationsPath { get { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + MIGRATIONS_DIR_NAME; } }

        private static readonly string QUERY_LOG_FILE_NAME = "wavebox_querylog.db";
        private static readonly string QUERY_LOG_SCHEMA_FILE_NAME = "wavebox_querylog.sql";
        public string QuerylogSchemaPath { get { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + QUERY_LOG_SCHEMA_FILE_NAME; } }
        public string QuerylogPath { get { return ServerUtility.RootPath() + QUERY_LOG_FILE_NAME; } }

        private static readonly object dbBackupLock = new object();
        public object DbBackupLock { get { return dbBackupLock; } }

        private bool isPoolingEnabled = true;

        public int Version {
            get {
                ISQLiteConnection conn = null;
                try {
                    conn = GetSqliteConnection();
                    return conn.ExecuteScalar<int>("SELECT VersionNumber FROM Version LIMIT 1");
                } catch (Exception e) {
                    logger.Error(e);
                } finally {
                    CloseSqliteConnection(conn);
                }

                return 0;
            }
        }

        // Sqlite connection pool
        private static readonly int MAX_CONNECTIONS = 100;
        private SQLiteConnectionPool mainPool;
        private SQLiteConnectionPool logPool;

        public Database() {
            mainPool = new SQLiteConnectionPool(MAX_CONNECTIONS, DatabasePath);
            logPool = new SQLiteConnectionPool(MAX_CONNECTIONS, QuerylogPath);
        }

        public void DatabaseSetup() {
            ApplySchemaIfEmpty(DATABASE_FILE_NAME, DatabaseSchemaPath, GetSqliteConnection, CloseSqliteConnection);
            ApplyMigrations();

            // The query log is a single table that has never changed, so it has no migrations
            ApplySchemaIfEmpty(QUERY_LOG_FILE_NAME, QuerylogSchemaPath, GetQueryLogSqliteConnection, CloseQueryLogSqliteConnection);
        }

        private void ApplyMigrations() {
            ISQLiteConnection conn = null;
            try {
                conn = GetSqliteConnection();
                DatabaseMigrator.Apply(conn, MigrationsPath);
            } catch (Exception e) {
                // Same reasoning as the schema apply: a half-migrated database only produces
                // confusing errors later, so surface it now
                logger.Error(e);
                throw;
            } finally {
                CloseSqliteConnection(conn);
            }
        }

        /// <summary>
        /// Creates a database from its bundled schema script if it has no tables yet. Checking for
        /// tables rather than for the file means an empty database left behind by an earlier
        /// connection still gets its schema, and makes repeat calls a no-op.
        /// </summary>
        private void ApplySchemaIfEmpty(string name, string schemaPath, Func<ISQLiteConnection> open, Action<ISQLiteConnection> close) {
            ISQLiteConnection conn = null;
            try {
                conn = open();

                if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'") > 0) {
                    return;
                }

                logger.IfInfo("Database " + name + " is empty; applying schema from " + schemaPath);

                conn.BeginTransaction();
                try {
                    conn.ExecuteScript(File.ReadAllText(schemaPath));
                    conn.Commit();
                } catch (Exception) {
                    conn.Rollback();
                    throw;
                }
            } catch (Exception e) {
                // Log before rethrowing so the failure lands in the server log, then fail loudly:
                // continuing with no schema only turns into confusing null references later on
                logger.Error(e);
                throw;
            } finally {
                close(conn);
            }
        }

        public ISQLiteConnection GetSqliteConnection() {
            if (isPoolingEnabled) {
                return mainPool.GetSqliteConnection();
            } else {
                ISQLiteConnection conn = new SQLite.SQLiteConnection(DatabasePath);
                conn.Execute("PRAGMA synchronous = OFF");
                // Five second busy timeout
                conn.BusyTimeout = new TimeSpan(0, 0, 5);
                return conn;
            }
        }

        public void CloseSqliteConnection(ISQLiteConnection conn) {
            if (isPoolingEnabled) {
                mainPool.CloseSqliteConnection(conn);
            } else {
                conn.Close();
            }
        }

        public ISQLiteConnection GetQueryLogSqliteConnection() {
            if (isPoolingEnabled) {
                return logPool.GetSqliteConnection();
            } else {
                ISQLiteConnection conn = new SQLite.SQLiteConnection(QuerylogPath);
                conn.Execute("PRAGMA synchronous = OFF");
                // Five second busy timeout
                conn.BusyTimeout = new TimeSpan(0, 0, 5);
                return conn;
            }
        }

        public void CloseQueryLogSqliteConnection(ISQLiteConnection conn) {
            if (isPoolingEnabled) {
                logPool.CloseSqliteConnection(conn);
            } else {
                conn.Close();
            }
        }

        public long LastQueryLogId {
            get {
                // Log the query
                ISQLiteConnection conn = null;
                try {
                    conn = GetQueryLogSqliteConnection();
                    return conn.ExecuteScalar<long>("SELECT MAX(QueryId) FROM QueryLog");
                } catch (Exception e) {
                    logger.Error(e);
                } finally {
                    CloseQueryLogSqliteConnection(conn);
                }

                return -1;
            }
        }

        public IList<QueryLog> QueryLogsSinceId(int queryId) {
            // Return all queries >= this id
            ISQLiteConnection conn = null;
            try {
                // Gather a list of queries from the query log, which can be used to synchronize a local database
                conn = GetQueryLogSqliteConnection();
                return conn.Query<QueryLog>("SELECT * FROM QueryLog WHERE QueryId >= ?", queryId);
            } catch (Exception e) {
                logger.Error(e);
            } finally {
                // Ensure database closed
                CloseQueryLogSqliteConnection(conn);
            }

            return new List<QueryLog>();
        }
    }
}
