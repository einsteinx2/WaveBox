using System;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;
using WaveBox.Transcoding;
using WaveBox.DataModel.Singletons;
using NLog;

namespace WaveBox
{
	public class WaveBoxService : System.ServiceProcess.ServiceBase
	{
		// Loggererererer... er.
		private static Logger logger = LogManager.GetCurrentClassLogger();

		// Instance of WaveBox, and the thread which will run it
		private Thread init;
		private WaveBoxMain wavebox;

		/// <summary>
		/// Constructor for WaveBox service.  Initializes the service and sets up the graceful shutdown
		/// </summary>
		public WaveBoxService()
		{
			logger.Info("[SERVICE] Initializing WaveBoxService");
			try
			{
				// Name the service
				this.ServiceName = "WaveBox";

				// Register shutdown handlers for Unix or Windows
				RegisterShutdownHandler();

				// Instantiate a WaveBox object
				wavebox = new WaveBoxMain();

				// Start it!
				this.OnStart();
			}
			// Catch any exceptions
			catch (Exception e)
			{
				logger.Error("[SERVICE(1)] {0}", e);
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
		///	OnStart launches the init thread with the WaveBox Start() function
		/// </summary>
		protected void OnStart()
		{
			logger.Info("[SERVICE] Starting...");

			// Launch the WaveBox thread using the Start() function from WaveBox
			init = new Thread(new ThreadStart(wavebox.Start));
			init.Start();
			logger.Info("[SERVICE] Started!");
		}

		/// <summary>
		/// OnStop stops the service, aborting the init thread, and terminating the program.  This replaces the
		/// ShutdownCommon function, as this will be the exit of the program.
		/// </summary>
		protected override void OnStop()
		{
			logger.Info("[SERVICE] Stopping...");

			// Abort main thread, nullify the WaveBox object
			init.Abort();

			// Shut off ZeroConf
			logger.Info("[SERVICE] Turning off ZeroConf...");
			wavebox.DisposeZeroConf();
			logger.Info("[SERVICE] ZeroConf off");

			// Stop any active transcodes
			logger.Info("[SERVICE] Cancelling any active transcodes...");
			TranscodeManager.Instance.CancelAllTranscodes();
			logger.Info("[SERVICE] All transcodes canceled");

			// Stop the file manager operation queue thread
			FileManager.Instance.Stop();

			// Stop the server
			wavebox.Stop();
			wavebox = null;

			logger.Info("[SERVICE] Stopped!");

			// Gracefully terminate
			Environment.Exit(0);
		}
	
		/// <summary>
		/// OnContinue does nothing yet
		/// </summary>
		protected override void OnContinue()
		{
			logger.Info("[SERVICE] Continuing");
		}

		/// <summary>
		/// OnPause does nothing yet
		/// </summary>
		protected override void OnPause()
		{
			logger.Info("[SERVICE] Pausing");
		}

		/// <summary>
		/// OnShutdown does nothing yet
		/// </summary>
		protected override void OnShutdown()
		{
			logger.Info("[SERVICE] Shutting down");
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
					logger.Info("Registering shutdown hook for Windows...");
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
	}
}
