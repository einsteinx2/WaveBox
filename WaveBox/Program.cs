using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bend.Util;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WaveBox.DataModel.Model;

namespace WaveBox
{
	class Program
	{
		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		static void Main(string[] args)
		{
			Console.WriteLine("[MAIN] Initializing WaveBox on {0} platform...", Environment.OSVersion.Platform.ToString());

			// Register the shutdown handler for the current platform
			RegisterShutdownHandler();

			// Start the HTTP server
			StartHTTPServer();
			
			// Perform initial setup of Settings, create a user
			Settings.SettingsSetup();
			User.CreateUser("test", "test");

			// Start file manager, calculate time it takes to run.
			var sw = new Stopwatch();
			Console.WriteLine("Scanning media directories...");
			sw.Start();
			FileManager.Instance.Setup();
			sw.Stop();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			Thread.Sleep(Timeout.Infinite);
		}

		/// <summary>
		/// Initialize the HTTP server thread.
		/// </summary>
		private static void StartHTTPServer()
		{
			// define run port
			int httpPort = 8080;

			// thread for the HTTP server.  its listen operation is blocking, so we can't start it before
			// we do any file scanning otherwise.
			Thread httpSrv = null;

			// Attempt to start the HTTP server thread
			try
			{
				var http = new WaveBoxHttpServer(httpPort);
				httpSrv = new Thread(new ThreadStart(http.Listen));
				httpSrv.Start();
			}
			// Catch any socket exceptions which occur
			catch (System.Net.Sockets.SocketException e)
			{
				// If the address is in use, WaveBox (or another service) is probably bound to that port; error out
				// For another sockets exception, just print the message
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
					Console.WriteLine("ERROR: Socket already in use.  Ensure that WaveBox is not already running.");
				else
					Console.WriteLine("ERROR: " + e.Message);

				// Quit with error return code
				Environment.Exit(-1);
			}
			// Catch any generic exception of non-Socket type
			catch (Exception e)
			{
				// Print the message, quit.
				Console.WriteLine(e.ToString());
				Environment.Exit(-1);
			}
		}

		/// <summary>
		/// Function to register shutdown handler for various platforms.  Windows uses its own, while UNIX variants
		/// use a specialized Unix shutdown handler.
		/// </summary>
		private static void RegisterShutdownHandler()
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
		private static bool ShutdownWindows(CtrlType sig)
		{
			// Trigger common shutdown function
			ShutdownCommon();
			return true;
		}

		/// <summary>
		/// Shutdown handler for UNIX systems, terminates WaveBox gracefully.
		/// </summary>
		private static void ShutdownUnix()
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
			ShutdownCommon();
		}

		/// <summary>
		/// Common shutdown actions.
		/// </summary>
		public static void ShutdownCommon()
		{
			Console.WriteLine("[MAIN] Executing shutdown hook!");
			/*Console.WriteLine("Executing shutdown hook!");
			SQLiteConnection dbconn = Database.GetDbConnection();
			dbconn.Close();

			if (dbconn.State == System.Data.ConnectionState.Closed)
			{
				Console.WriteLine("Database connection successfully closed");
			}

			else Console.WriteLine("Database connection failed to close");*/

			// Gracefully terminate
			Environment.Exit(0);
		}
	}
}
