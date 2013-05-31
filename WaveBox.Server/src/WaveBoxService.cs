using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Web;
using Mono.Unix.Native;
using Mono.Unix;
using WaveBox.Static;
using WaveBox.Transcoding;

namespace WaveBox
{
	public class WaveBoxService : System.ServiceProcess.ServiceBase
	{
		// Loggererererer... er.
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// WaveBox temporary folder, for transcodes and such
		public static string TempFolder = Path.GetTempPath() + "wavebox";

		// Gather metrics about WaveBox instance
		// Operating system platform
		public static string Platform { get; set; }
		// Current version of WaveBox, from assembly
		public static string BuildVersion { get; set; }
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
				switch (Utility.DetectOS())
				{
					case Utility.OS.Windows:
						Platform = "Windows";
						break;
					case Utility.OS.MacOSX:
						Platform = "Mac OS X";
						break;
					case Utility.OS.Linux:
						Platform = "Linux";
						break;
					case Utility.OS.BSD:
						Platform = "BSD";
						break;
					case Utility.OS.Solaris:
						Platform = "Solaris";
						break;
					case Utility.OS.Unix:
						Platform = "Unix";
						break;
					default:
						Platform = "unknown";
						break;
				}

				// Store version
				var assembly = Assembly.GetExecutingAssembly().GetName();
				BuildVersion = String.Format("{0}.{1}.{2}.{3}", assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build, assembly.Version.Revision);

				// Build date detection
				BuildDate = Utility.GetBuildDate();

				if (logger.IsInfoEnabled) logger.Info("BuildDate timestamp: " + BuildDate.ToUniversalUnixTimestamp());

				// Get start up time
				StartTime = DateTime.Now;

				// Create WaveBox's temporary folder
				if (!Directory.Exists(TempFolder))
				{
					Directory.CreateDirectory(TempFolder);
					if (logger.IsInfoEnabled) logger.Info("Created temp folder: " + TempFolder);
				}

				// Instantiate a WaveBox object
				wavebox = new WaveBoxMain();

				// Start it!
				this.OnStart();
			}
			// Handle any uncaught exceptions
			catch (Exception e)
			{
				//logger.Error(e);
				Utility.ReportCrash(e, false);
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
			FileManager.Stop();

			// Destroy temp folder
			if (Directory.Exists(TempFolder))
			{
				// Count of files deleted
				int i = 0;

				// Remove any files in folder
				foreach (string f in Directory.GetFiles(TempFolder))
				{
					File.Delete(f);
					i++;
				}

				// Remove folder
				Directory.Delete(TempFolder);

				if (logger.IsInfoEnabled) logger.Info("Deleted temp folder: " + TempFolder + " (" + i + " files)");
			}


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
	}
}
