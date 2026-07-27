using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;

namespace WaveBox.Static {
    /// <summary>
    /// Applies the ordered SQL migrations in res/migrations to a database, tracking how far it has
    /// got in the single-row Version table.
    ///
    /// res/wavebox.sql is a frozen baseline at version 0; every schema change since is a migration,
    /// so a fresh database is the seed plus every migration replayed in order. Keeping one path
    /// means the migrations are exercised on every fresh install and every CI run, rather than
    /// only ever being tried against a real database on someone's server.
    /// </summary>
    internal static class DatabaseMigrator {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        // 00001_add_something.sql -- zero padded so lexical and numeric order agree at a glance,
        // though ordering below is numeric regardless
        private static readonly Regex FileNamePattern = new Regex(@"^(\d{5})_.+\.sql$", RegexOptions.Compiled);

        internal sealed class Migration {
            internal int Version { get; set; }
            internal string Path { get; set; }
            internal string Name { get; set; }
        }

        /// <summary>
        /// Reads the migrations directory, ordered by version. Throws on a malformed file name or a
        /// duplicate version, so a typo fails loudly at startup instead of silently not running.
        /// Gaps in numbering are fine -- branches merge out of order.
        /// </summary>
        internal static IList<Migration> Discover(string directory) {
            List<Migration> migrations = new List<Migration>();

            if (!Directory.Exists(directory)) {
                return migrations;
            }

            Dictionary<int, string> seen = new Dictionary<int, string>();

            foreach (string path in Directory.GetFiles(directory, "*.sql")) {
                string name = System.IO.Path.GetFileName(path);

                Match match = FileNamePattern.Match(name);
                if (!match.Success) {
                    throw new InvalidOperationException(
                        "Migration file name '" + name + "' is malformed; expected a 5 digit version, " +
                        "an underscore and a description, e.g. 00001_add_user_nickname.sql");
                }

                int version = Int32.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                string existing;
                if (seen.TryGetValue(version, out existing)) {
                    throw new InvalidOperationException(
                        "Migrations '" + existing + "' and '" + name + "' share version " + version +
                        "; renumber one of them");
                }
                seen[version] = name;

                migrations.Add(new Migration { Version = version, Path = path, Name = name });
            }

            migrations.Sort((a, b) => a.Version.CompareTo(b.Version));

            return migrations;
        }

        /// <summary>
        /// Brings a database up to the newest bundled migration. Each migration and its version bump
        /// share a transaction, so a failure part way through a chain leaves the database sitting
        /// cleanly at the last migration that did succeed.
        /// </summary>
        internal static void Apply(ISQLiteConnection conn, string directory) {
            IList<Migration> migrations = Discover(directory);

            int current = CurrentVersion(conn);
            int newest = migrations.Count > 0 ? migrations[migrations.Count - 1].Version : 0;

            if (current > newest) {
                throw new InvalidOperationException(
                    "Database is at schema version " + current + " but this build only knows up to " +
                    newest + "; it was created by a newer version of WaveBox. Upgrade WaveBox, or " +
                    "delete the database to start fresh.");
            }

            foreach (Migration migration in migrations) {
                if (migration.Version <= current) {
                    continue;
                }

                logger.IfInfo("Applying database migration " + migration.Name);

                conn.BeginTransaction();
                try {
                    conn.ExecuteScript(File.ReadAllText(migration.Path));
                    SetVersion(conn, migration.Version);
                    conn.Commit();
                } catch (Exception e) {
                    conn.Rollback();
                    logger.Error("Migration " + migration.Name + " failed; database left at version " + current, e);
                    throw;
                }

                current = migration.Version;
            }
        }

        /// <summary>
        /// The schema version, treating a missing row as 0. Databases created before versioning
        /// existed have the table but no row in it.
        /// </summary>
        internal static int CurrentVersion(ISQLiteConnection conn) {
            if (conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Version") == 0) {
                return 0;
            }

            return conn.ExecuteScalar<int>("SELECT VersionNumber FROM Version LIMIT 1");
        }

        private static void SetVersion(ISQLiteConnection conn, int version) {
            // Replace rather than update, so a database missing its row heals itself
            conn.Execute("DELETE FROM Version");
            conn.Execute("INSERT INTO Version (VersionNumber) VALUES (?)", version);
        }
    }
}
