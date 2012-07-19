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
		// Store operating system platform for signal handling
		static string platform = Environment.OSVersion.Platform.ToString();

		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		static void Main(string[] args)
		{
			// Initialize on given platform
			Console.WriteLine("[MAIN] Initializing WaveBox on {0} platform...", platform);

			// define run port
			int httpPort = 8080;

			// thread for the HTTP server.  its listen operation is blocking, so we can't start it before
			// we do any file scanning otherwise.
			Thread httpSrv = null;

			// If we're on UNIX, register the shutdown in a new thread
			if(platform == "Unix")
				new Thread(Shutdown).Start();
				
			/*// register application kill notifier
			Console.WriteLine("Registering shutdown hook...");
			_handler += new EventHandler(Handler);
			SetConsoleCtrlHandler(_handler, true);*/

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
			
			// Perform initial setup of Settings, create a user
			Settings.SettingsSetup();
			User.CreateUser("test", "test");

			//GC.Collect();

			// Start file manager, calculate time it takes to run.
			var sw = new Stopwatch();
			Console.WriteLine("Scanning media directories...");
			sw.Start();
			FileManager.Instance.Setup();
			sw.Stop();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			Thread.Sleep(Timeout.Infinite);
		}

		/*[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

		private delegate bool EventHandler(CtrlType sig);
		static EventHandler _handler;

		enum CtrlType
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT = 1,
			CTRL_CLOSE_EVENT = 2,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT = 6
		}

		private static bool Handler(CtrlType sig)
		{
			Shutdown();
			return true;
		}*/

		/// <summary>
		/// Shutdown handler terminates WaveBox gracefully.
		/// </summary>
		public static void Shutdown()
		{
			// When on UNIX platform, register the following signals:
			// SIGINT -> Ctrl+C
			// SIGTERM -> kill or killall
			if(platform == "Unix")
			{
				UnixSignal[] unixSignals = new UnixSignal[]
				{
					new UnixSignal(Signum.SIGINT),
					new UnixSignal(Signum.SIGTERM),
				};

				// Block until one of the aforementioned signals is issued, then continue shutdown
				UnixSignal.WaitAny(unixSignals, -1);
			}

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
