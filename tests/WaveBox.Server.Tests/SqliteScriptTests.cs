using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Xunit;

namespace WaveBox.Server.Tests {
    /// <summary>
    /// ExecuteScript runs a whole SQL script through SQLite's own parser. Every case below except
    /// the first breaks a splitter that cuts the text on ';', which is what this replaced.
    /// </summary>
    public class SqliteScriptTests : IDisposable {
        private readonly string dbPath;
        private readonly SQLite.SQLiteConnection conn;

        public SqliteScriptTests() {
            dbPath = Path.Combine(Path.GetTempPath(), "wavebox-script-" + Guid.NewGuid().ToString("N") + ".db");
            conn = new SQLite.SQLiteConnection(dbPath);
        }

        public void Dispose() {
            conn.Dispose();
            File.Delete(dbPath);
        }

        [Fact]
        public void RunsEveryStatementNotJustTheFirst() {
            conn.ExecuteScript(@"
                CREATE TABLE A (x INTEGER);
                CREATE TABLE B (y INTEGER);
                INSERT INTO A VALUES (1);
                INSERT INTO A VALUES (2);
            ");

            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'B'"));
            Assert.Equal(2, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM A"));
        }

        [Fact]
        public void PreservesSemicolonsInsideStringLiterals() {
            conn.ExecuteScript(@"
                CREATE TABLE T (v TEXT);
                INSERT INTO T VALUES ('a;b;c');
                INSERT INTO T VALUES ('it''s; quoted');
            ");

            Assert.Equal("a;b;c", conn.ExecuteScalar<string>("SELECT v FROM T ORDER BY rowid LIMIT 1"));
            Assert.Equal("it's; quoted", conn.ExecuteScalar<string>("SELECT v FROM T ORDER BY rowid DESC LIMIT 1"));
        }

        [Fact]
        public void IgnoresSemicolonsInLineAndBlockComments() {
            conn.ExecuteScript(@"
                -- a comment; with a semicolon
                CREATE TABLE T (x INTEGER);
                /* another; one
                   spanning lines; too */
                INSERT INTO T VALUES (42);
            ");

            Assert.Equal(42, conn.ExecuteScalar<int>("SELECT x FROM T"));
        }

        [Fact]
        public void HandlesTriggerBodiesContainingSemicolons() {
            conn.ExecuteScript(@"
                CREATE TABLE Src (x INTEGER);
                CREATE TABLE Log (x INTEGER);
                CREATE TRIGGER SrcInsert AFTER INSERT ON Src
                BEGIN
                    INSERT INTO Log VALUES (NEW.x);
                    UPDATE Log SET x = x * 2;
                END;
                INSERT INTO Src VALUES (5);
            ");

            Assert.Equal(10, conn.ExecuteScalar<int>("SELECT x FROM Log"));
        }

        [Fact]
        public void RoundTripsNonAsciiText() {
            // The script is marshalled as UTF-8 by byte length; the older string overload passed a
            // character count as a byte count, which truncates as soon as the text is not ASCII
            conn.ExecuteScript(@"
                CREATE TABLE T (v TEXT);
                INSERT INTO T VALUES ('Björk – Jóga ⟨æ⟩');
            ");

            Assert.Equal("Björk – Jóga ⟨æ⟩", conn.ExecuteScalar<string>("SELECT v FROM T"));
        }

        [Fact]
        public void ToleratesTrailingWhitespaceAndComments() {
            conn.ExecuteScript("CREATE TABLE T (x INTEGER);\n-- trailing comment\n   \n");

            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'T'"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \n  ")]
        [InlineData("-- nothing but a comment")]
        public void EmptyScriptsAreNoOps(string script) {
            conn.ExecuteScript(script);

            Assert.Equal(0, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'"));
        }

        [Fact]
        public void ReportsTheFailingStatement() {
            SQLite.SQLiteException error = Assert.Throws<SQLite.SQLiteException>(() => conn.ExecuteScript(@"
                CREATE TABLE T (x INTEGER);
                INSERT INTO NotATable VALUES (1);
            "));

            Assert.Contains("NotATable", error.Message);

            // The statements before the failure still ran; the caller owns the transaction
            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'T'"));
        }

        [Fact]
        public void RollsBackWhenTheCallerOwnsATransaction() {
            conn.ExecuteScript("CREATE TABLE Keep (x INTEGER);");

            conn.BeginTransaction();
            try {
                conn.ExecuteScript(@"
                    CREATE TABLE Dropped (x INTEGER);
                    INSERT INTO NotATable VALUES (1);
                ");
            } catch (SQLite.SQLiteException) {
                conn.Rollback();
            }

            Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'Keep'"));
            Assert.Equal(0, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'Dropped'"));
        }
    }
}
