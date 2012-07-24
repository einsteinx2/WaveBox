using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.HttpServer;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using WaveBox.DataModel.Model;

namespace WaveBox
{
	class WaveBoxMain
	{
		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		public void Start()
		{
			Console.WriteLine("[WAVEBOX] Initializing WaveBox on {0} platform...", Environment.OSVersion.Platform.ToString());

			// Start the HTTP server
			StartHTTPServer();
			
			// Perform initial setup of Settings, create a user
			Settings.SettingsSetup();
			User.CreateUser("test", "test");

			// Start file manager, calculate time it takes to run.
			var sw = new Stopwatch();
			Console.WriteLine("[WAVEBOX] Scanning media directories...");
			sw.Start();
			FileManager.Instance.Setup();
			sw.Stop();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			Thread.Sleep(Timeout.Infinite);

			return;
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
				httpSrv.IsBackground = true;
				httpSrv.Start();
			}
			// Catch any socket exceptions which occur
			catch (System.Net.Sockets.SocketException e)
			{
				// If the address is in use, WaveBox (or another service) is probably bound to that port; error out
				// For another sockets exception, just print the message
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
					Console.WriteLine("[WAVEBOX] ERROR: Socket already in use.  Ensure that WaveBox is not already running.");
				else
					Console.WriteLine("[WAVEBOX] ERROR: " + e.Message);

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
	}
}
