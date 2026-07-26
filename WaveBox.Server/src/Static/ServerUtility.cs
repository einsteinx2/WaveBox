using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using WaveBox.Core;

namespace WaveBox {
    public static class ServerUtility {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ServerUtility));

        // Enumerations for operating system
        public enum OS {
            Windows,
            MacOSX,
            Linux,
            BSD,
            Solaris,
            Unix,
            Unknown,
        }

        // Retrieve string description from OS enumeration (AOT-safe, no attribute reflection)
        public static string ToDescription(this OS value) {
            switch (value) {
            case OS.Windows:
                return "Windows";
            case OS.MacOSX:
                return "Mac OS X";
            case OS.Linux:
                return "Linux";
            case OS.BSD:
                return "BSD";
            case OS.Solaris:
                return "Solaris";
            case OS.Unix:
                return "Unix";
            default:
                return "Unknown";
            }
        }

        /// <summary>
        /// Detect the host operating system (kept as an enum for API response compatibility)
        /// </summary>
        public static OS DetectOS() {
            if (OperatingSystem.IsWindows()) {
                return OS.Windows;
            }
            if (OperatingSystem.IsMacOS()) {
                return OS.MacOSX;
            }
            if (OperatingSystem.IsLinux()) {
                return OS.Linux;
            }
            if (OperatingSystem.IsFreeBSD()) {
                return OS.BSD;
            }
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                return OS.Unix;
            }
            return OS.Unknown;
        }

        /// <summary>
        /// Returns the UTC date on which WaveBox was built, embedded at compile time via the
        /// BuildDate assembly metadata attribute (the old PE-header trick breaks under deterministic builds).
        /// </summary>
        public static DateTime GetBuildDate() {
            AssemblyMetadataAttribute attr = typeof(ServerUtility).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "BuildDate");

            DateTime buildDate;
            if (attr != null && DateTime.TryParse(attr.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out buildDate)) {
                return buildDate;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Retrieve the server's GUID for URL forwarding, or generate a new one if none exists
        /// </summary>
        public static string GetServerGuid() {
            string guid = null;

            ISQLiteConnection conn = null;
            try {
                // Grab server GUID from the database
                conn = Injection.Get<IDatabase>().GetSqliteConnection();
                guid = conn.ExecuteScalar<string>("SELECT Guid FROM Server");
            } catch (Exception e) {
                logger.Error("Exception loading server GUID", e);
            } finally {
                Injection.Get<IDatabase>().CloseSqliteConnection(conn);
            }

            // If it doesn't exist, generate a new one
            if ((object)guid == null) {
                // Generate the GUID
                Guid guidObj = Guid.NewGuid();
                guid = guidObj.ToString();

                // Store the GUID in the database
                try {
                    conn = Injection.Get<IDatabase>().GetSqliteConnection();
                    int affected = conn.Execute("INSERT INTO Server (Guid) VALUES (?)", guid);

                    if (affected == 0) {
                        guid = null;
                    }
                } catch (Exception e) {
                    logger.Error("Exception saving guid", e);
                    guid = null;
                } finally {
                    Injection.Get<IDatabase>().CloseSqliteConnection(conn);
                }
            }

            return guid;
        }

        /// <summary>
        /// Retrieve the server's forwarding URL from database
        /// </summary>
        public static string GetServerUrl() {
            ISQLiteConnection conn = null;
            try {
                // Grab server URL from the database
                conn = Injection.Get<IDatabase>().GetSqliteConnection();
                return conn.ExecuteScalar<string>("SELECT Url FROM Server");
            } catch (Exception e) {
                logger.Error("Exception loading server info", e);
            } finally {
                Injection.Get<IDatabase>().CloseSqliteConnection(conn);
            }

            return null;
        }

        /// <summary>
        /// Called whenever WaveBox encounters a fatal error, resulting in a crash.  When configured, this will automatically
        /// report the exception to WaveBox's crash dump service.  If not configured, the exception will be dumped to the log,
        /// and the user may choose to report it manually.
        /// </summary>
        public static void ReportCrash(Exception exception, bool terminateProcess) {
            logger.Error("WaveBox has crashed!");

            // Report crash if enabled
            if (Injection.Get<IServerSettings>().CrashReportEnable) {
                logger.Error("ReportCrash called", exception);

                try {
                    using (HttpClient client = new HttpClient()) {
                        var content = new FormUrlEncodedContent(new[] {
                            new System.Collections.Generic.KeyValuePair<string, string>("exception", exception.ToString())
                        });
                        HttpResponseMessage response = client.PostAsync("http://crash.waveboxapp.com", content).GetAwaiter().GetResult();
                        logger.Error("Crash report server response: " + response.StatusCode);
                    }
                } catch (Exception reportException) {
                    logger.Error("Failed to submit crash report: " + reportException.Message);
                }
            } else {
                // If automatic reporting disabled, print the exception so user has the option of sending crash dump manually
                logger.Error("Automatic crash reporting is disabled, dumping exception...");
                logger.Error("---------------- CRASH DUMP ----------------");
                logger.Error(exception);
                logger.Error("-------------- END CRASH DUMP --------------");
                logger.Error("Please report this exception on: https://github.com/einsteinx2/WaveBox/issues");
            }

            if (terminateProcess) {
                System.Environment.FailFast("Unhandled exception caught, bailing as we're now in an unknown state.");
            }
        }

        /// <summary>
        /// Detects WaveBox's executable path
        /// </summary>
        public static string ExecutablePath() {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Detects WaveBox's root directory, for storing per-user configuration
        /// </summary>
        public static string RootPath() {
            // WAVEBOX_ROOT overrides the per-OS default, primarily so tests can isolate the config
            // and databases (the Windows default under CommonApplicationData can't be redirected via HOME)
            string overrideRoot = Environment.GetEnvironmentVariable("WAVEBOX_ROOT");
            if (!String.IsNullOrEmpty(overrideRoot)) {
                return overrideRoot.EndsWith(Path.DirectorySeparatorChar) ? overrideRoot : overrideRoot + Path.DirectorySeparatorChar;
            }

            // Note: SpecialFolder.Personal means $HOME/Documents on modern .NET, but meant $HOME under
            // Mono — use UserProfile so existing installs keep their config/database locations.
            switch (DetectOS()) {
            case OS.Windows:
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\WaveBox\\";
            case OS.MacOSX:
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Application Support/WaveBox/";
            case OS.Linux:
            case OS.BSD:
            case OS.Solaris:
            case OS.Unix:
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.wavebox/";
            default:
                return "";
            }
        }
    }
}
