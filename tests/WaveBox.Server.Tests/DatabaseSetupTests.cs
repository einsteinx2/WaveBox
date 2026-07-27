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

        private static int QuerylogScalar(string sql) {
            IDatabase db = Injection.Get<IDatabase>();
            ISQLiteConnection conn = null;
            try {
                conn = db.GetQueryLogSqliteConnection();
                return conn.ExecuteScalar<int>(sql);
            } finally {
                db.CloseQueryLogSqliteConnection(conn);
            }
        }

        [Fact]
        public void SetupCreatesDatabasesInRoot() {
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox.db")));
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox_querylog.db")));
        }

        [Fact]
        public void SetupAppliesFullSchemaFromScript() {
            // The whole script has to run, not just its first statement: a partial apply would
            // still produce a file and a User table, so assert on tables declared near the end
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'User'"));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Favorite'"));
            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'MusicBrainzCheckDate'"));
            Assert.Equal(1, QuerylogScalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'QueryLog'"));
        }

        [Fact]
        public void SetupSeedsLookupTables() {
            // These rows came from the old prebuilt wavebox.db template; they now come from the
            // INSERTs in wavebox.sql, and nothing else would notice if they went missing
            Assert.Equal(12, Scalar("SELECT COUNT(*) FROM ItemType"));
            Assert.Equal(10, Scalar("SELECT COUNT(*) FROM FileType"));
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
            Assert.Equal(12, Scalar("SELECT COUNT(*) FROM ItemType"));
            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox.db")));
        }

        [Fact]
        public void SetupLeavesTheDatabaseAtTheNewestBundledMigration() {
            // The seed is a frozen baseline at 0, so a fresh database ends up at whatever the
            // highest migration in res/migrations is -- 0 while that directory is still empty
            int expected = 0;
            foreach (string path in Directory.GetFiles(Injection.Get<IDatabase>().MigrationsPath, "*.sql")) {
                expected = Math.Max(expected, Int32.Parse(Path.GetFileName(path).Substring(0, 5)));
            }

            Assert.Equal(1, Scalar("SELECT COUNT(*) FROM Version"));
            Assert.Equal(expected, Scalar("SELECT VersionNumber FROM Version"));
        }

        [Fact]
        public void SetupFindsTheBundledMigrationsDirectory() {
            // Guards the csproj copy: if res/migrations stops being deployed next to the binary,
            // migrations would silently never run rather than failing
            Assert.True(Directory.Exists(Injection.Get<IDatabase>().MigrationsPath),
                        "res/migrations was not copied to the output directory");
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
