using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using Xunit;

namespace WaveBox.Server.Tests {
    /// <summary>
    /// Exercises the migrator directly against a throwaway database and migrations directory, so
    /// none of this depends on the bundled res/migrations content (which is empty today).
    /// </summary>
    public class DatabaseMigratorTests : IDisposable {
        private readonly string workDir;
        private readonly string migrationsDir;
        private readonly string dbPath;
        private readonly SQLite.SQLiteConnection conn;

        public DatabaseMigratorTests() {
            workDir = Directory.CreateTempSubdirectory("wavebox-migrations-").FullName;
            migrationsDir = Path.Combine(workDir, "migrations");
            Directory.CreateDirectory(migrationsDir);

            dbPath = Path.Combine(workDir, "test.db");
            conn = new SQLite.SQLiteConnection(dbPath);
            conn.ExecuteScript("CREATE TABLE Version (VersionNumber INTEGER NOT NULL); INSERT INTO Version VALUES (0);");
        }

        public void Dispose() {
            conn.Dispose();
            try {
                Directory.Delete(workDir, true);
            } catch (IOException) {
            }
        }

        private void WriteMigration(string name, string sql) {
            File.WriteAllText(Path.Combine(migrationsDir, name), sql);
        }

        private bool ColumnExists(string table, string column) {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM pragma_table_info('" + table + "') WHERE name = '" + column + "'") > 0;
        }

        // --- Discover -------------------------------------------------------

        [Fact]
        public void DiscoverOrdersNumericallyNotLexically() {
            WriteMigration("00010_ten.sql", "SELECT 1;");
            WriteMigration("00002_two.sql", "SELECT 1;");
            WriteMigration("00001_one.sql", "SELECT 1;");

            IList<DatabaseMigrator.Migration> found = DatabaseMigrator.Discover(migrationsDir);

            Assert.Equal(new int[] { 1, 2, 10 }, found.Select(m => m.Version).ToArray());
        }

        [Fact]
        public void DiscoverIgnoresNonSqlFiles() {
            WriteMigration("00001_one.sql", "SELECT 1;");
            File.WriteAllText(Path.Combine(migrationsDir, "README.md"), "not a migration");
            File.WriteAllText(Path.Combine(migrationsDir, "notes.txt"), "also not");

            Assert.Single(DatabaseMigrator.Discover(migrationsDir));
        }

        [Fact]
        public void DiscoverReturnsEmptyForMissingDirectory() {
            Assert.Empty(DatabaseMigrator.Discover(Path.Combine(workDir, "does-not-exist")));
        }

        [Theory]
        [InlineData("1_short_version.sql")]
        [InlineData("000001_six_digits.sql")]
        [InlineData("00001-wrong-separator.sql")]
        [InlineData("00001_.sql")]
        [InlineData("no_version_at_all.sql")]
        public void DiscoverThrowsOnMalformedFileName(string name) {
            WriteMigration(name, "SELECT 1;");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => DatabaseMigrator.Discover(migrationsDir));

            Assert.Contains(name, error.Message);
        }

        [Fact]
        public void DiscoverThrowsOnDuplicateVersion() {
            WriteMigration("00001_one.sql", "SELECT 1;");
            WriteMigration("00001_one_again.sql", "SELECT 1;");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => DatabaseMigrator.Discover(migrationsDir));

            Assert.Contains("share version 1", error.Message);
        }

        [Fact]
        public void DiscoverAllowsGapsInNumbering() {
            WriteMigration("00001_one.sql", "SELECT 1;");
            WriteMigration("00007_seven.sql", "SELECT 1;");

            Assert.Equal(new int[] { 1, 7 }, DatabaseMigrator.Discover(migrationsDir).Select(m => m.Version).ToArray());
        }

        // --- Apply ----------------------------------------------------------

        [Fact]
        public void ApplyRunsPendingMigrationsInOrderAndRecordsVersion() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER);");
            WriteMigration("00001_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");
            WriteMigration("00002_add_c.sql", "ALTER TABLE T ADD COLUMN c INTEGER;");

            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.True(ColumnExists("T", "b"));
            Assert.True(ColumnExists("T", "c"));
            Assert.Equal(2, DatabaseMigrator.CurrentVersion(conn));
        }

        [Fact]
        public void ApplySkipsMigrationsAtOrBelowCurrentVersion() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER); DELETE FROM Version; INSERT INTO Version VALUES (1);");
            // Would fail if it ran, since column a already exists
            WriteMigration("00001_already_applied.sql", "ALTER TABLE T ADD COLUMN a INTEGER;");
            WriteMigration("00002_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");

            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.True(ColumnExists("T", "b"));
            Assert.Equal(2, DatabaseMigrator.CurrentVersion(conn));
        }

        [Fact]
        public void ApplyIsANoOpWhenAlreadyCurrent() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER);");
            WriteMigration("00001_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");

            DatabaseMigrator.Apply(conn, migrationsDir);
            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.Equal(1, DatabaseMigrator.CurrentVersion(conn));
        }

        [Fact]
        public void ApplyRollsBackAFailingMigrationAndStopsAtTheLastGoodVersion() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER);");
            WriteMigration("00001_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");
            WriteMigration("00002_broken.sql", "ALTER TABLE T ADD COLUMN c INTEGER;\nINSERT INTO NotATable VALUES (1);");
            WriteMigration("00003_add_d.sql", "ALTER TABLE T ADD COLUMN d INTEGER;");

            Assert.ThrowsAny<Exception>(() => DatabaseMigrator.Apply(conn, migrationsDir));

            Assert.True(ColumnExists("T", "b"));
            // 00002 is atomic, so its first statement is rolled back too, and 00003 never runs
            Assert.False(ColumnExists("T", "c"));
            Assert.False(ColumnExists("T", "d"));
            Assert.Equal(1, DatabaseMigrator.CurrentVersion(conn));
        }

        [Fact]
        public void ApplyThrowsWhenTheDatabaseIsNewerThanTheBuild() {
            conn.ExecuteScript("DELETE FROM Version; INSERT INTO Version VALUES (9);");
            WriteMigration("00001_one.sql", "SELECT 1;");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => DatabaseMigrator.Apply(conn, migrationsDir));

            Assert.Contains("newer version of WaveBox", error.Message);
        }

        [Fact]
        public void ApplyTreatsAMissingVersionRowAsZero() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER); DELETE FROM Version;");
            WriteMigration("00001_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");

            Assert.Equal(0, DatabaseMigrator.CurrentVersion(conn));

            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.True(ColumnExists("T", "b"));
            Assert.Equal(1, DatabaseMigrator.CurrentVersion(conn));
        }

        [Fact]
        public void ApplyKeepsASingleVersionRow() {
            conn.ExecuteScript("CREATE TABLE T (a INTEGER);");
            WriteMigration("00001_add_b.sql", "ALTER TABLE T ADD COLUMN b INTEGER;");
            WriteMigration("00002_add_c.sql", "ALTER TABLE T ADD COLUMN c INTEGER;");

            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Version"));
        }

        [Fact]
        public void ApplyHandlesMultiStatementMigrations() {
            // The SQLite table-rebuild dance, which is the realistic shape of a non-trivial migration
            conn.ExecuteScript("CREATE TABLE T (a INTEGER, b TEXT); INSERT INTO T VALUES (1, 'keep;me');");
            WriteMigration("00001_rebuild.sql", @"
                CREATE TABLE T_new (a INTEGER, b TEXT, c INTEGER DEFAULT 0);
                INSERT INTO T_new (a, b) SELECT a, b FROM T;
                DROP TABLE T;
                ALTER TABLE T_new RENAME TO T;
            ");

            DatabaseMigrator.Apply(conn, migrationsDir);

            Assert.True(ColumnExists("T", "c"));
            Assert.Equal("keep;me", conn.ExecuteScalar<string>("SELECT b FROM T"));
            Assert.Equal(1, DatabaseMigrator.CurrentVersion(conn));
        }
    }
}
