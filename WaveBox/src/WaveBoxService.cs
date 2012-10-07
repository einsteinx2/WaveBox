using System;
using System.ServiceProcess;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;
using WaveBox.Transcoding;

namespace WaveBox
{
	public class WaveBoxService : System.ServiceProcess.ServiceBase
	{
		// Instance of WaveBox, and the thread which will run it
		private Thread init;
		private WaveBoxMain wavebox;

		/// <summary>
		/// Constructor for WaveBox service.  Initializes the service and sets up the graceful shutdown
		/// </summary>
		public WaveBoxService()
		{
			Console.WriteLine("[SERVICE] Initializing WaveBoxService");
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
			catch(Exception e)
			{
				Console.WriteLine("[SERVICE(1)] {0}", e);
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
			Console.WriteLine("[SERVICE] Starting...");

			// Launch the WaveBox thread using the Start() function from WaveBox
			init = new Thread(new ThreadStart(wavebox.Start));
			init.Start();
			Console.WriteLine("[SERVICE] Started!");
		}

		/// <summary>
		/// OnStop stops the service, aborting the init thread, and terminating the program.  This replaces the
		/// ShutdownCommon function, as this will be the exit of the program.
		/// </summary>
		protected override void OnStop()
		{
			Console.WriteLine("[SERVICE] Stopping...");

			// Abort main thread, nullify the WaveBox object
			init.Abort();
			wavebox = null;

			Console.WriteLine("[SERVICE] Cancelling any active transcodes...");
			TranscodeManager.Instance.CancelAllTranscodes();
			Console.WriteLine("[SERVICE] All transcodes canceled");

			Console.WriteLine("[SERVICE] Turning off ZeroConf...");
			WaveBoxMain.DisposeZeroConf();
			Console.WriteLine("[SERVICE] ZeroConf off");

			Console.WriteLine("[SERVICE] Stopped!");

			// Gracefully terminate
			Environment.Exit(0);
		}
	
		/// <summary>
		/// OnContinue does nothing yet
		/// </summary>
		protected override void OnContinue()
		{
			Console.WriteLine("[SERVICE] Continuing");
		}

		/// <summary>
		/// OnPause does nothing yet
		/// </summary>
		protected override void OnPause()
		{
			Console.WriteLine("[SERVICE] Pausing");
		}

		/// <summary>
		/// OnShutdown does nothing yet
		/// </summary>
		protected override void OnShutdown()
		{
			Console.WriteLine("[SERVICE] Shutting down");
		}
		
		/// <summary>
		/// Function to register shutdown handler for various platforms.  Windows uses its own, while UNIX variants
		/// use a specialized Unix shutdown handler.
		/// </summary>
		private void RegisterShutdownHandler()
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
		        case PlatformID.Win32S:
		        case PlatformID.Win32Windows:
					// register application kill notifier
					Console.WriteLine("Registering shutdown hook for Windows...");
					windowsShutdownHandler += new EventHandler(ShutdownWindows);
					SetConsoleCtrlHandler(windowsShutdownHandler, true);
					break;
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

		static public OS DetectOS()
		{ 
			if (System.IO.Path.DirectorySeparatorChar == '\\')
			{
				return OS.Windows;
			} 
			else if (IsMacOSX())
			{
				return OS.MacOSX;
			} 
			else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
			{
				return OS.Unix;
			}
			return OS.unknown; 
		}

		static private bool IsMacOSX()
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
