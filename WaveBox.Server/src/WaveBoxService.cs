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
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Derived;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Server;
using WaveBox.Server.Extensions;
using WaveBox.Service;
using WaveBox.Service.Services.FileManager;
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
		// Operating system enumeration
		public static ServerUtility.OS OS { get; set; }
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
			logger.IfInfo("Initializing WaveBoxService");
			try
			{
				// Name the service
				this.ServiceName = "WaveBox";

				// Register shutdown handlers for Unix or Windows
				this.RegisterShutdownHandler();

				// Detect operating system
				OS = ServerUtility.DetectOS();

				// Now that platform is detected, inject platform-specific classes
				InjectPlatformSpecificClasses();

				// Store version
				var assembly = Assembly.GetExecutingAssembly().GetName();
				BuildVersion = String.Format("{0}.{1}.{2}.{3}", assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build, assembly.Version.Revision);

				// Build date detection
				BuildDate = ServerUtility.GetBuildDate();

				logger.IfInfo("BuildDate timestamp: " + BuildDate.ToUnixTime());

				// Get start up time
				StartTime = DateTime.UtcNow;

				// Create WaveBox's temporary folder
				if (!Directory.Exists(TempFolder))
				{
					Directory.CreateDirectory(TempFolder);
					logger.IfInfo("Created temp folder: " + TempFolder);
				}

				// Instantiate a WaveBox object
				wavebox = new WaveBoxMain();

				// Start it!
				this.OnStart();
			}
			// Handle any uncaught exceptions
			catch (Exception e)
			{
				ServerUtility.ReportCrash(e, false);
			}
		}

		/// <summary>
		/// Perform dependency injection for all other classes in WaveBox
		/// </summary>
		private static void InjectClasses()
		{
			// Core
			Injection.Kernel.Bind<IDatabase>().To<Database>().InSingletonScope();
			Injection.Kernel.Bind<IServerSettings>().To<ServerSettings>().InSingletonScope();
			Injection.Kernel.Bind<IPodcastShim>().To<PodcastShim>().InSingletonScope();

			// Load Server
			Injection.Kernel.Load(new ServerModule());
		}

		/// <summary>
		/// Perform dependency injection for all classes which have platform-specific implementations
		/// </summary>
		private static void InjectPlatformSpecificClasses()
		{
			// IWebClient - used to download strings and files using HTTP, with a timeout in milliseconds
			int timeout = 5000;

			// Linux WebRequest libraries are really bad, so we have our own implementation which calls curl
			if (OS == ServerUtility.OS.Linux)
			{
				Injection.Kernel.Bind<IWebClient>().To<LinuxWebClient>().WithConstructorArgument("timeout", timeout);
			}
			else
			{
				// All other operating systems use derived TimedWebClient
				Injection.Kernel.Bind<IWebClient>().To<TimedWebClient>().WithConstructorArgument("timeout", timeout);
			}

			// IFileManager - used to scan and keep track of file changes

			// Mac OSX has a better filesystem watching facility called FSEvents, so we make use of that here
			if (OS == ServerUtility.OS.MacOSX)
			{
				Injection.Kernel.Bind<IFileManager>().To<MacOSXFileManager>().InSingletonScope();
			}
			else
			{
				Injection.Kernel.Bind<IFileManager>().To<FileManager>().InSingletonScope();
			}
		}

		/// <summary>
		/// Service entry point.  Starts the WaveBox Service, which will then launch the application
		/// </summary>
		static void Main(string[] args)
		{
			// Setup the dependency injection
			InjectClasses();

			WaveBoxService service = new WaveBoxService();
			if (Environment.UserInteractive)
			{
				// Allow use to run as a regular console program on Windows
				Console.WriteLine("Press enter to exit");
				Console.ReadLine();
			}
			else
			{
				// Create an instance of the service, run it!
				ServiceBase[] serviceBase = new ServiceBase[] { service };
				ServiceBase.Run(serviceBase);
			}
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
			logger.IfInfo("Starting...");

			// Launch the WaveBox thread using the Start() function from WaveBox
			init = new Thread(new ThreadStart(wavebox.Start));
			init.Start();
			logger.IfInfo("Started!");
		}

		/// <summary>
		/// OnStop stops the service, aborting the init thread, and terminating the program.  This replaces the
		/// ShutdownCommon function, as this will be the exit of the program.
		/// </summary>
		protected override void OnStop()
		{
			logger.IfInfo("Stopping...");

			// Abort main thread, nullify the WaveBox object
			init.Abort();

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

				logger.IfInfo("Deleted temp folder: " + TempFolder + " (" + i + " files)");
			}

			// Stop the server
			wavebox.Stop();
			wavebox = null;

			logger.IfInfo("Stopped!");

			// Gracefully terminate
			Environment.Exit(0);
		}

		/// <summary>
		/// OnContinue does nothing yet
		/// </summary>
		protected override void OnContinue()
		{
			logger.IfInfo("Continuing");
		}

		/// <summary>
		/// OnPause does nothing yet
		/// </summary>
		protected override void OnPause()
		{
			logger.IfInfo("Pausing");
		}

		/// <summary>
		/// OnShutdown does nothing yet
		/// </summary>
		protected override void OnShutdown()
		{
			logger.IfInfo("Shutting down");
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
					logger.IfInfo("Registering shutdown hook for Windows...");
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
