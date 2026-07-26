using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class DatabaseSetupTests : IDisposable {
        private readonly IntegrationHarness harness;

        public DatabaseSetupTests() {
            harness = new IntegrationHarness();
        }

        public void Dispose() {
            harness.Dispose();
        }

        private static int Scalar(string sql) {
            IDatabase db = Injection.Get<IDatabase>();
            ISQLiteConnection conn = null;
            try {
                conn = db.GetSqliteConnection();
                return conn.ExecuteScalar<int>(sql);
            } finally {
                db.CloseSqliteConnection(conn);
            }
        }

        private static void Execute(string sql) {
            IDatabase db = Injection.Get<IDatabase>();
            ISQLiteConnection conn = null;
            try {
                conn = db.GetSqliteConnection();
                conn.Execute(sql);
            } finally {
                db.CloseSqliteConnection(conn);
            }
        }

        [Fact]
        public void SetupCopiesTemplateDatabasesIntoRoot() {
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox.db")));
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox_querylog.db")));
        }

        [Fact]
        public void SetupAddsApiKeyColumnAndUniqueIndex() {
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM pragma_table_info('User') WHERE name = 'ApiKey'"));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'user_ApiKey'"));
        }

        [Fact]
        public void SetupIsIdempotent() {
            IDatabase db = Injection.Get<IDatabase>();

            db.DatabaseSetup();
            db.DatabaseSetup();

            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM pragma_table_info('User') WHERE name = 'ApiKey'"));
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox.db")));
        }

        [Fact]
        public void UpgradeSchemaRestoresApiKeyOnPreMigrationDatabase() {
            // Simulate a database created before the ApiKey migration
            Execute("DROP INDEX user_ApiKey");
            Execute("ALTER TABLE User DROP COLUMN ApiKey");
            Assert.Equal(0, Scalar("SELECT COUNT(*) FROM pragma_table_info('User') WHERE name = 'ApiKey'"));

            IDatabase db = Injection.Get<IDatabase>();
            db.DatabaseSetup();

            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM pragma_table_info('User') WHERE name = 'ApiKey'"));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'user_ApiKey'"));

            // Running the migration again on an already-upgraded schema is a no-op
            db.DatabaseSetup();
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM pragma_table_info('User') WHERE name = 'ApiKey'"));
        }

        [Fact]
        public void ApiKeyUniqueIndexRejectsDuplicateKeys() {
            IUserRepository users = Injection.Get<IUserRepository>();
            User first = users.CreateUser("first", "pw", Role.User, null);
            User second = users.CreateUser("second", "pw", Role.User, null);

            Assert.True(first.UpdateApiKey("shared-key"));

            // The unique index makes the raw UPDATE throw; UpdateApiKey swallows it and reports false
            Assert.False(second.UpdateApiKey("shared-key"));
            Assert.Null(users.UserForName("second").ApiKey);
        }

        [Fact]
        public void QueryLogRecordsLoggedWrites() {
            // CreateUser goes through InsertObject -> logged writes land in the query log DB
            Injection.Get<IUserRepository>().CreateUser("logged", "pw", Role.User, null);

            Assert.True(Injection.Get<IDatabase>().LastQueryLogId > 0);
        }
    }
}
