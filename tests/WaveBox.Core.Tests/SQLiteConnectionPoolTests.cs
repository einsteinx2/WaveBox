using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;
using Xunit;

namespace WaveBox.Core.Tests {
    // Each test creates its own pool over its own temp database file, so these are safe to run
    // in parallel and touch no process-global state.
    //
    // GetSqliteConnection() recurses forever if no connection is available, so every call that
    // could conceivably block is run on a Task and guarded with a timeout — a regression then
    // fails the test in seconds instead of hanging the run.
    public class SQLiteConnectionPoolTests : IDisposable {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly string databasePath;

        public SQLiteConnectionPoolTests() {
            databasePath = Path.Combine(Path.GetTempPath(), "wavebox-pool-test-" + Guid.NewGuid().ToString("N") + ".db");
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            try {
                File.Delete(databasePath);
            } catch {
                // Best effort cleanup of the temp database
            }
        }

        private static Task<ISQLiteConnection> GetWithTimeout(SQLiteConnectionPool pool) {
            // WaitAsync throws TimeoutException if the pool never hands out a connection
            return Task.Run(() => pool.GetSqliteConnection())
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task GetSqliteConnection_ReturnsUsableConnection() {
            SQLiteConnectionPool pool = new SQLiteConnectionPool(2, databasePath);

            ISQLiteConnection conn = await GetWithTimeout(pool);
            Assert.NotNull(conn);
            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT 1"));

            pool.CloseSqliteConnection(conn);
        }

        [Fact]
        public async Task CloseSqliteConnection_ReturnsConnectionToPoolForReuse() {
            SQLiteConnectionPool pool = new SQLiteConnectionPool(2, databasePath);

            ISQLiteConnection first = await GetWithTimeout(pool);
            pool.CloseSqliteConnection(first);

            // The pooled connection is handed back rather than a new one being opened
            ISQLiteConnection second = await GetWithTimeout(pool);
            Assert.Same(first, second);

            pool.CloseSqliteConnection(second);
        }

        [Fact]
        public async Task CloseAllConnections_InvokesActionOnceDrained() {
            SQLiteConnectionPool pool = new SQLiteConnectionPool(2, databasePath);

            // Prime the pool with one previously used connection
            pool.CloseSqliteConnection(await GetWithTimeout(pool));

            bool actionRan = false;
            await Task.Run(() => pool.CloseAllConnections(() => { actionRan = true; }), TestContext.Current.CancellationToken)
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.True(actionRan);
        }

        [Fact]
        public async Task CloseAllConnections_WaitsForOutstandingConnections() {
            SQLiteConnectionPool pool = new SQLiteConnectionPool(2, databasePath);
            ISQLiteConnection conn = await GetWithTimeout(pool);

            bool actionRan = false;
            Task closeAll = Task.Run(() => pool.CloseAllConnections(() => { actionRan = true; }), TestContext.Current.CancellationToken);

            // While a connection is outstanding, the backup action must not run
            await Task.Delay(300, TestContext.Current.CancellationToken);
            Assert.False(actionRan, "CloseAllConnections ran its action while a connection was still in use");

            pool.CloseSqliteConnection(conn);
            await closeAll.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.True(actionRan);
        }

        [Fact]
        public async Task GetSqliteConnection_SucceedsAgainAfterCloseAllConnections() {
            // Pins the fixed behavior: CloseAllConnections re-enables connection handout when it
            // finishes.  Before the fix, getConnectionsAllowed stayed false forever and this Get
            // would recurse/sleep endlessly — the timeout makes that regression fail fast.
            SQLiteConnectionPool pool = new SQLiteConnectionPool(2, databasePath);
            pool.CloseSqliteConnection(await GetWithTimeout(pool));

            await Task.Run(() => pool.CloseAllConnections(() => { }), TestContext.Current.CancellationToken)
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);

            ISQLiteConnection conn = await GetWithTimeout(pool);
            Assert.NotNull(conn);
            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT 1"));
            pool.CloseSqliteConnection(conn);
        }
    }
}
