using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Net;
using System.Web;

namespace WaveBox.Static
{
	public static class Utility
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/*
		 * Static Methods
		 */

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
		/// Called whenever WaveBox encounters a fatal error, resulting in a crash.  When configured, this will automatically
		/// report the exception to WaveBox's crash dump service.  If not configured, the exception will be dumped to the log,
		/// and the user may choose to report it manually.
		/// </summary>
		public static void ReportCrash(Exception exception, bool terminateProcess) 
		{
			logger.Error("WaveBox has crashed!");

			// Report crash if enabled
			if (Settings.CrashReportEnable)
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

		/// <summary>
		/// Generates a random string, for use in session creation
		/// </summary>
		static private Random rng = new Random();
		public static string RandomString(int size)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()";

			char[] buffer = new char[size];
			for (int i = 0; i < size; i++)
			{
				buffer[i] = chars[rng.Next(chars.Length)];
			}
			return new string(buffer);
		}

		/*
		 * Class Extensions
		 */

		/// <summary>
		/// Determine if a string is meant to indicate true, return false if none detected
		/// </summary>
		public static bool IsTrue(this string boolString)
		{
			try
			{
				// Null string -> false
				if (boolString == null)
				{
					return false;
				}

				// Lowercase and trim whitespace
				boolString = boolString.ToLower();
				boolString = boolString.Trim();

				if (boolString.Length > 0)
				{
					// t or 1 -> true
					if (boolString[0] == 't' || boolString[0] == '1')
					{
						return true;
					}
				}

				// Anything else, false
				return false;
			}
			catch
			{
				// Exception, false
				return false;
			}
		}

		/// <summary>
		/// Generates a MD5 sum of a given string
		/// <summary>
		public static string MD5(this string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(sumthis)), 0);
		}

		/// <summary>
		/// Returns an integer representation of a month string
		/// </summary>
		public static int MonthForAbbreviation(this string abb)
		{
			switch (abb.ToLower())
			{
				case "jan": return 1;
				case "feb": return 2;
				case "mar": return 3;
				case "apr": return 4;
				case "may": return 5;
				case "jun": return 6;
				case "jul": return 7;
				case "aug": return 8;
				case "sep": return 9;
				case "oct": return 10;
				case "nov": return 11;
				case "dec": return 12;
				default: return 0;
			}
		}

		/// <summary>
		/// Generates a SHA1 sum of a given string
		/// </summary>
		public static string SHA1(this string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(provider.ComputeHash(Encoding.ASCII.GetBytes(sumthis))).Replace("-", "");
		}

		public static string ToRFC1123(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT";
		}

		/// <summary>
		/// Creates universal DateTime object from an input UNIX timestamp
		/// </summary>
		public static DateTime ToDateTimeFromUnixTimestamp(this long unixTime)
		{
			return new DateTime(1970, 1, 1).AddSeconds(unixTime).ToUniversalTime();
		}

		/// <summary>
		/// Creates a GMT UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToUniversalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
		}

		/// <summary>
		/// Creates a local UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToLocalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1).ToUniversalTime()).TotalSeconds;
		}

		/// <summary>
		/// Convert a List to a quoted CSV string
		/// </summary>
		public static string ToCSV(this IList<string> list, bool quoted = false)
		{
			string buffer = "";

			// If list is empty, return empty list
			if (list.Count == 0)
			{
				if (quoted)
				{
					return "\"\"";
				}

				return "";
			}

			foreach (string s in list)
			{
				if (quoted)
				{
					buffer += "\"" + s + "\", ";
				}
				else
				{
					buffer += s + ", ";
				}
			}

			return buffer.Trim(new char[] {' ', ','});
		}
	}
}

