using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Static;
using WaveBox.Core;

namespace WaveBox
{
	public static class ServerUtility
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Adapted from here: http://mono.1490590.n4.nabble.com/Howto-detect-os-td1549244.html
		[DllImport("libc")]
		static extern int uname(IntPtr buf);
		public enum OS {Windows, MacOSX, Linux, BSD, Solaris, Unix, unknown};

		/// <summary>
		/// DetectOS uses a couple different tricks to detect if we are running on Windows, Mac OSX, or Unix.
		/// </summary>
		public static OS DetectOS()
		{
			// Detect Windows via directory separator character
			if (System.IO.Path.DirectorySeparatorChar == '\\')
			{
				return OS.Windows;
			}
			else
			{
				// Call uname to determine if we're running on a UNIX variant
				// In theory, this could be any platform on which Mono runs, so we check a lot of cases.
				switch (KernelUname())
				{
					// Darwin - Mac OSX
					case "Darwin":
					return OS.MacOSX;
					// Linux
					case "Linux":
					return OS.Linux;
					// BSD and friends - BSD
					case "DragonFly":
					case "FreeBSD":
					case "GNU/kFreeBSD":
					case "OpenBSD":
					case "NetBSD":
					return OS.BSD;
					// SunOS - Solaris
					case "SunOS":
					return OS.Solaris;
					default:
					break;
				}

				// Last resort, check for platform ID values historically linked to UNIX
				int platformId = (int)Environment.OSVersion.Platform;
				if (platformId == 4 || platformId == 6 || platformId == 128)
				{
					return OS.Unix;
				}
			}

			// If no matching cases, OS is unknown
			return OS.unknown;
		}

		/// <summary>
		/// Calls the system's uname function, to return the name of the current kernel (e.g. Darwin (Mac OSX), Linux,
		/// FreeBSD, etc.)
		/// </summary>
		public static string KernelUname()
		{
			IntPtr buf = IntPtr.Zero;
			string kernel = "";

			try
			{
				buf = Marshal.AllocHGlobal(8192);
				// This is a hacktastic way of getting sysname from uname()
				if (uname(buf) == 0)
				{
					kernel = Marshal.PtrToStringAnsi(buf);
				}
			}
			catch
			{

			}
			finally
			{
				if (buf != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(buf);
				}
			}

			return kernel;
		}

		// Borrowed from: http://stackoverflow.com/questions/1600962/displaying-the-build-date
		/// <summary>
		/// Returns a DateTime object containing the date on which WaveBox was compiled (good for nightly build names,
		/// as well as information on reporting issues which may occur later on.
		/// </summary>
		public static DateTime GetBuildDate()
		{
			// Read the PE header to get build date
			string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;
			byte[] b = new byte[2048];
			System.IO.Stream s = null;

			try
			{
				s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				s.Read(b, 0, 2048);
			}
			finally
			{
				if (s != null)
				{
					s.Close();
				}
			}

			int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
			int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds(secondsSince1970);
			dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
			return dt;
		}

		/// <summary>
		/// Retrieve the server's GUID for URL forwarding, or generate a new one if none exists
		/// </summary>
		public static string GetServerGuid()
		{
			string guid = null;

			ISQLiteConnection conn = null;
			try
			{
				// Grab server GUID from the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				guid = conn.ExecuteScalar<string>("SELECT Guid FROM Server");
			}
			catch (Exception e)
			{
				logger.Error("Exception loading server GUID", e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			// If it doesn't exist, generate a new one
			if ((object)guid == null)
			{
				// Generate the GUID
				Guid guidObj = Guid.NewGuid();
				guid = guidObj.ToString();

				// Store the GUID in the database
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					int affected = conn.Execute("INSERT INTO Server (Guid) VALUES (?)", guid);

					if (affected == 0)
					{
						guid = null;
					}
				}
				catch (Exception e)
				{
					logger.Error("Exception saving guid", e);
					guid = null;
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}
			}

			return guid;
		}

		/// <summary>
		/// Retrieve the server's forwarding URL from database
		/// </summary>
		public static string GetServerUrl()
		{
			ISQLiteConnection conn = null;
			try
			{
				// Grab server URL from the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<string>("SELECT Url FROM Server");
			}
			catch (Exception e)
			{
				logger.Error("Exception loading server info", e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return null;
		}

		/// <summary>
		/// Called whenever WaveBox encounters a fatal error, resulting in a crash.  When configured, this will automatically
		/// report the exception to WaveBox's crash dump service.  If not configured, the exception will be dumped to the log,
		/// and the user may choose to report it manually.
		/// </summary>
		public static void ReportCrash(Exception exception, bool terminateProcess)
		{
			logger.Error("WaveBox has crashed!");

			// Report crash if enabled
			if (Injection.Kernel.Get<IServerSettings>().CrashReportEnable)
			{
				logger.Error("ReportCrash called", exception);

				// Submit to the web service
				Uri URI = new Uri("http://crash.waveboxapp.com");
				string parameters = "exception=" + HttpUtility.UrlEncode(exception.ToString());

				using (WebClient wc = new WebClient())
				{
					wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

					if (terminateProcess)
					{
						// We're about to terminate, so send it synchronously
						string response = wc.UploadString(URI, parameters);
						logger.Error("Crash report server response: " + response);
					}
					else
					{
						// We're stayin' alive, stayin' alive, so send it asynchronously
						wc.UploadStringCompleted += new UploadStringCompletedEventHandler((sender, e) => logger.Error("Crash report server async response: " + e.Result));
						wc.UploadStringAsync(URI, parameters);
					}
				}
			}
			else
			{
				// If automatic reporting disabled, print the exception so user has the option of sending crash dump manually
				logger.Error("Automatic crash reporting is disabled, dumping exception...");
				logger.Error("---------------- CRASH DUMP ----------------");
				logger.Error(exception);
				logger.Error("-------------- END CRASH DUMP --------------");
				logger.Error("Please report this exception on: https://github.com/einsteinx2/WaveBox/issues");
			}

			if (terminateProcess)
			{
				System.Environment.FailFast("Unhandled exception caught, bailing as we're now in an unknown state.");
			}
		}

		private static void ReportCrashAsyncCallback(object sender, UploadStringCompletedEventArgs e)
		{
			logger.Error("Crash report server async response: " + e.Result);
		}

		/// <summary>
		/// Detects WaveBox's executable path
		/// </summary>
		public static string ExecutablePath()
		{
			return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
		}

		/// <summary>
		/// Detects WaveBox's root directory, for storing per-user configuration
		/// </summary>
		public static string RootPath()
		{
			switch (DetectOS())
			{
				case OS.Windows:
					return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\WaveBox\\";
				case OS.MacOSX:
					return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Application Support/WaveBox/";
				case OS.Linux:
				case OS.BSD:
				case OS.Solaris:
				case OS.Unix:
					return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/.wavebox/";
				default:
					return "";
			}
		}

		public static string CallerMethodName()
		{
			StackTrace stackTrace = new StackTrace();
			StackFrame stackFrame = stackTrace.GetFrame(2);
			MethodBase methodBase = stackFrame.GetMethod();
			return methodBase.Name;
		}
	}
}
