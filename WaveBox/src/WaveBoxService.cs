using System;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;
using WaveBox.Transcoding;
using WaveBox.Singletons;
using System.Text;
using System.Net;
using System.Web;

namespace WaveBox
{
	public class WaveBoxService : System.ServiceProcess.ServiceBase
	{
		// Loggererererer... er.
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Gather metrics about WaveBox instance
		// Operating system platform
		public static string Platform { get; set; }
		// DateTime object containing the build date of WaveBox (for versioning, status metric)
		public static DateTime BuildDate { get; set; }
		// DateTime object containing start time, used to calculate uptime
		public static DateTime StartTime { get; set; }

		// Instance of WaveBox, and the thread which will run it
		private Thread init;
		private WaveBoxMain wavebox;

		/// <summary>
		/// Constructor for WaveBox service.  Initializes the service and sets up the graceful shutdown
		/// </summary>
		public WaveBoxService()
		{
			if (logger.IsInfoEnabled) logger.Info("Initializing WaveBoxService");
			try
			{
				// Name the service
				this.ServiceName = "WaveBox";

				// Register shutdown handlers for Unix or Windows
				RegisterShutdownHandler();

				// Gather some metrics about this instance of WaveBox
				// Operating system detection
				switch (DetectOS())
				{
					case OS.Windows:
						Platform = "Windows";
						break;
					case OS.MacOSX:
						Platform = "Mac OS X";
						break;
					case OS.Unix:
						Platform = "UNIX/Linux";
						break;
					default:
						Platform = "unknown";
						break;
				}

				// Build date detection
				BuildDate = GetBuildDate();

				if (logger.IsInfoEnabled) logger.Info("BuildDate timestamp: " + BuildDate.ToUniversalUnixTimestamp());

				// Get start up time
				StartTime = DateTime.Now;

				// Instantiate a WaveBox object
				wavebox = new WaveBoxMain();

				// Start it!
				this.OnStart();
			}
			// Handle any uncaught exceptions
			catch (Exception e)
			{
				//logger.Error(e);
				WaveBoxService.ReportCrash(e, false);
			}
		}

		/// <summary>
		/// Service entry point.  Starts the WaveBox Service, which will then launch the application
		/// </summary>
		static void Main(string[] args)
		{
			// Create an instance of the service, run it!
			ServiceBase[] service = new ServiceBase[] { new WaveBoxService() };
			ServiceBase.Run(service);
		}

		/// <summary>
		/// Override for OnStart from base service class.  We don't need CLI args, so we just call our own
		/// </summary>
		protected override void OnStart(string[] args)
		{
			this.OnStart();
		}

		/// <summary>
		/// OnStart launches the init thread with the WaveBox Start() function
		/// </summary>
		protected void OnStart()
		{
			if (logger.IsInfoEnabled) logger.Info("Starting...");

			// Launch the WaveBox thread using the Start() function from WaveBox
			init = new Thread(new ThreadStart(wavebox.Start));
			init.Start();
			if (logger.IsInfoEnabled) logger.Info("Started!");
		}

		/// <summary>
		/// OnStop stops the service, aborting the init thread, and terminating the program.  This replaces the
		/// ShutdownCommon function, as this will be the exit of the program.
		/// </summary>
		protected override void OnStop()
		{
			if (logger.IsInfoEnabled) logger.Info("Stopping...");

			// Abort main thread, nullify the WaveBox object
			init.Abort();

			// Shut off ZeroConf
			if (logger.IsInfoEnabled) logger.Info("Turning off ZeroConf...");
			ZeroConf.DisposeZeroConf();
			if (logger.IsInfoEnabled) logger.Info("ZeroConf off");

			// Stop any active transcodes
			if (logger.IsInfoEnabled) logger.Info("Cancelling any active transcodes...");
			TranscodeManager.Instance.CancelAllTranscodes();
			if (logger.IsInfoEnabled) logger.Info("All transcodes canceled");

			// Stop the file manager operation queue thread
			FileManager.Instance.Stop();

			// Stop the server
			wavebox.Stop();
			wavebox = null;

			if (logger.IsInfoEnabled) logger.Info("Stopped!");

			// Gracefully terminate
			Environment.Exit(0);
		}
	
		/// <summary>
		/// OnContinue does nothing yet
		/// </summary>
		protected override void OnContinue()
		{
			if (logger.IsInfoEnabled) logger.Info("Continuing");
		}

		/// <summary>
		/// OnPause does nothing yet
		/// </summary>
		protected override void OnPause()
		{
			if (logger.IsInfoEnabled) logger.Info("Pausing");
		}

		/// <summary>
		/// OnShutdown does nothing yet
		/// </summary>
		protected override void OnShutdown()
		{
			if (logger.IsInfoEnabled) logger.Info("Shutting down");
		}
		
		/// <summary>
		/// Function to register shutdown handler for various platforms.  Windows uses its own, while UNIX variants
		/// use a specialized Unix shutdown handler.
		/// </summary>
		private void RegisterShutdownHandler()
		{
			switch (Environment.OSVersion.Platform)
			{
				// Windows
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
					// register application kill notifier
					if (logger.IsInfoEnabled) logger.Info("Registering shutdown hook for Windows...");
					windowsShutdownHandler += new EventHandler(ShutdownWindows);
					SetConsoleCtrlHandler(windowsShutdownHandler, true);
					break;
				// UNIX
				case PlatformID.Unix:
				case PlatformID.MacOSX:
					new Thread(ShutdownUnix).Start();
					break;
			}
		}

		/// <summary>
		/// Shutdown handler for Windows systems, terminates WaveBox gracefully.
		/// </summary>
		[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
		private delegate bool EventHandler(CtrlType sig);
		static EventHandler windowsShutdownHandler;
		enum CtrlType
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT = 1,
			CTRL_CLOSE_EVENT = 2,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT = 6
		}
		private bool ShutdownWindows(CtrlType sig)
		{
			// Trigger common shutdown function
			this.OnStop();
			return true;
		}

		/// <summary>
		/// Shutdown handler for UNIX systems, terminates WaveBox gracefully.
		/// </summary>
		private void ShutdownUnix()
		{
			// When on UNIX platform, register the following signals:
			// SIGINT -> Ctrl+C
			// SIGTERM -> kill or killall
			UnixSignal[] unixSignals = new UnixSignal[]
			{
				new UnixSignal(Signum.SIGINT),
				new UnixSignal(Signum.SIGTERM),
			};

			// Block until one of the aforementioned signals is issued, then continue shutdown
			UnixSignal.WaitAny(unixSignals, -1);

			// Trigger common shutdown function once unblocked
			this.OnStop();
		}

		// Adapted from here: http://mono.1490590.n4.nabble.com/Howto-detect-os-td1549244.html
		[DllImport("libc")] 
		static extern int uname(IntPtr buf); 
		public enum OS {Windows, MacOSX, Unix, unknown};

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
			// Detect MacOSX using a uname hack
			else if (IsMacOSX())
			{
				return OS.MacOSX;
			}
			// Detect Unix via OS platform
			else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
			{
				return OS.Unix;
			}
			
			// If no matching cases, OS is unknown
			return OS.unknown; 
		}

		/// <summary>
		/// IsMacOSX uses a uname hack to determine if the operating system is MacOSX
		/// </summary>
		private static bool IsMacOSX()
		{ 
			IntPtr buf = IntPtr.Zero; 
			try
			{ 
				buf = Marshal.AllocHGlobal(8192); 
				// This is a hacktastic way of getting sysname from uname() 
				if (uname(buf) == 0)
				{ 
					string os = Marshal.PtrToStringAnsi(buf); 
					if (os == "Darwin")
					{
						return true;
					} 
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
			return false; 
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
	}
}
