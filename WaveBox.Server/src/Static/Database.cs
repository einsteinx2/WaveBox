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
            ApplySchemaIfEmpty(QUERY_LOG_FILE_NAME, QuerylogSchemaPath, GetQueryLogSqliteConnection, CloseQueryLogSqliteConnection);
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
                    foreach (string statement in ReadSchemaStatements(schemaPath)) {
                        conn.Execute(statement);
                    }
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

        /// <summary>
        /// Splits a schema script into individual statements. The vendored sqlite-net only executes
        /// one statement per call and exposes no sqlite3_exec, so the script has to be split here.
        /// Naive splitting on ';' is safe only because these files are sqlite3 .dump output whose
        /// string literals contain no semicolons -- do not point this at hand-written SQL.
        ///
        /// A .dump also wraps itself in BEGIN TRANSACTION/COMMIT; those are dropped so the caller
        /// owns the transaction and a failure part way through rolls the whole thing back.
        /// </summary>
        private static IEnumerable<string> ReadSchemaStatements(string schemaPath) {
            foreach (string statement in File.ReadAllText(schemaPath).Split(';')) {
                string trimmed = statement.Trim();
                if (trimmed.Length == 0 || IsTransactionControl(trimmed)) {
                    continue;
                }

                yield return trimmed;
            }
        }

        private static bool IsTransactionControl(string statement) {
            return statement.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase)
                || statement.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase)
                || statement.StartsWith("ROLLBACK", StringComparison.OrdinalIgnoreCase);
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
