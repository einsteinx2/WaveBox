using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using WaveBox.Http;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using WaveBox.DataModel.Model;
using WaveBox.Transcoding;
using Mono.Zeroconf;

namespace WaveBox
{
	class WaveBoxMain
	{
		public static RegisterService ZeroConfService { get; set; }

		public static string RootPath()
		{
			/*foreach (Environment.SpecialFolder val in Enum.GetValues(typeof(Environment.SpecialFolder)))
			{
				Console.WriteLine(val + ": " + Environment.GetFolderPath(val));
			}*/
			
			switch (WaveBoxService.DetectOS())
			{
				case WaveBoxService.OS.Windows:
					return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\WaveBox\\";
				case WaveBoxService.OS.MacOSX:
					return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Application Support/WaveBox/";
				case WaveBoxService.OS.Unix:
					return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/.wavebox/";
				default:
					return "";
			}
		}

		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		public void Start()
		{
			Console.WriteLine("[WAVEBOX] Initializing WaveBox on {0} platform...", Environment.OSVersion.Platform.ToString());

			if (!Directory.Exists(RootPath()))
			{
				Directory.CreateDirectory(RootPath());
			}

			// Perform initial setup of Settings, create a user
			Database.DatabaseSetup();
			Settings.SettingsSetup();

			// Start the HTTP server
			StartHTTPServer();
			//PublishZeroConf();

			TranscodeManager.Instance.Setup();

			User.CreateUser("test", "test");

			// Start file manager, calculate time it takes to run.
			Console.WriteLine("[WAVEBOX] Scanning media directories...");
			FileManager.Instance.Setup();

            // Start podcast download queue
            PodcastManagement.DownloadQueue.FeedChecks.queueOperation(new FeedCheckOperation(0));
            PodcastManagement.DownloadQueue.FeedChecks.startScanQueue();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			//Thread.Sleep(Timeout.Infinite);

			return;
		}

		public static void PublishZeroConf()
		{
			if ((object)ZeroConfService == null)
			{
				try
				{
					ZeroConfService = new RegisterService();
					ZeroConfService.Name = System.Environment.MachineName;
					//ZeroConfService.Name = "WaveBox on " + System.Environment.MachineName;
					//ZeroConfService.Name = "WaveBox";
					ZeroConfService.RegType = "_wavebox._tcp";
					ZeroConfService.ReplyDomain = "local.";
					ZeroConfService.Port = (short)Settings.Port;
					
					TxtRecord record = new TxtRecord();
					record.Add ("URL", "http://something.wavebox.es");
					ZeroConfService.TxtRecord = record;
					
					ZeroConfService.Register();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					DisposeZeroConf();
				}
			}
		}

		public static void DisposeZeroConf()
		{
			if ((object)ZeroConfService != null)
			{
				ZeroConfService.Dispose();
				ZeroConfService = null;
			}
		}

		/// <summary>
		/// Initialize the HTTP server thread.
		/// </summary>
		private static void StartHTTPServer()
		{
			// thread for the HTTP server.  its listen operation is blocking, so we can't start it before
			// we do any file scanning otherwise.
			Thread httpSrv = null;

			// Attempt to start the HTTP server thread
			try
			{
				HttpServer http = new HttpServer(Settings.Port);
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
					Console.WriteLine("[WAVEBOX(1)] ERROR: Socket already in use.  Ensure that WaveBox is not already running.");
				else
					Console.WriteLine("[WAVEBOX(2)] ERROR: " + e);

				// Quit with error return code
				Environment.Exit(-1);
			}
			// Catch any generic exception of non-Socket type
			catch (Exception e)
			{
				// Print the message, quit.
				Console.WriteLine("[WAVEBOX(3)] ERROR: " + e);
				Environment.Exit(-1);
			}
		}
	}
}
