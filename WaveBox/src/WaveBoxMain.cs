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
using NLog;
using System.Net;
using System.Net.Sockets;

namespace WaveBox
{
	class WaveBoxMain
	{	
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public RegisterService ZeroConfService { get; set; }

		public string ServerGuid { get; set; }

		public string ServerUrl { get; set; }

		private HttpServer httpServer;

		public static string RootPath()
		{
			/*foreach (Environment.SpecialFolder val in Enum.GetValues(typeof(Environment.SpecialFolder)))
			{
				logger.Info(val + ": " + Environment.GetFolderPath(val));
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

		private void ServerSetup()
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM server", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					ServerGuid = reader.GetStringOrNull(reader.GetOrdinal("guid"));
					ServerUrl = reader.GetStringOrNull(reader.GetOrdinal("url"));
				}
			}
			catch(Exception e)
			{
				logger.Error("[WAVEBOX] exception loading server info" + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			// If it doesn't exist, generate a new one
			if ((object)ServerGuid == null)
			{
				// Generate the Guid
				Guid guid = Guid.NewGuid();
				ServerGuid = guid.ToString();

				// Store the Guid
				try
				{
					conn = Database.GetDbConnection();
					IDbCommand q = Database.GetDbCommand("INSERT INTO server (guid) VALUES (@guid)", conn);
					q.AddNamedParam("@guid", ServerGuid);
					q.Prepare();
					if (q.ExecuteNonQuery() == 0)
					{
						ServerGuid = null;
					}
				}
				catch(Exception e)
				{
					logger.Error("[WAVEBOX] exception saving guid" + e);
					ServerGuid = null;
				}
				finally
				{
					Database.Close(conn, null);
				}
			}
		}

		private static IPAddress LocalIPAddress()
		{
			if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			{
				return null;
			}
			
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			
			return host
				.AddressList
				.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
		}

		public void RegisterUrl()
		{
			if ((object)ServerUrl != null)
			{
				string urlString = "http://register.wavebox.es" + 
					"?host=" + Uri.EscapeUriString(ServerUrl) + 
					"&serverId=" + Uri.EscapeUriString(ServerGuid) + 
					"&port=" + Settings.Port + 
					"&isSecure=0" + 
					"&localIp=" + LocalIPAddress().ToString();

				Console.WriteLine("registering url: " + urlString);

				WebClient client = new WebClient();
				client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(RegisterUrlCompleted);
				client.DownloadDataAsync(new Uri(urlString));
			}
		}

		private static void RegisterUrlCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			// Do nothing for now, check for success and handle failures later
		}

		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		public void Start()
		{
			logger.Info("[WAVEBOX] Initializing WaveBox on {0} platform...", Environment.OSVersion.Platform.ToString());

			if (!Directory.Exists(RootPath()))
			{
				Directory.CreateDirectory(RootPath());
			}

			// Perform initial setup of Settings, create a user
			Database.DatabaseSetup();
			Settings.SettingsSetup();
			ServerSetup();
			RegisterUrl();

			// Start the HTTP server
			StartHTTPServer();
			//PublishZeroConf();

			TranscodeManager.Instance.Setup();

			User.CreateUser("test", "test");

			// Start file manager, calculate time it takes to run.
			logger.Info("[WAVEBOX] Scanning media directories...");
			FileManager.Instance.Setup();

            // Start podcast download queue
            PodcastManagement.DownloadQueue.FeedChecks.queueOperation(new FeedCheckOperation(0));
            PodcastManagement.DownloadQueue.FeedChecks.startScanQueue();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			//Thread.Sleep(Timeout.Infinite);

			return;
		}

		public void PublishZeroConf()
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
					logger.Info(e);
					DisposeZeroConf();
				}
			}
		}

		public void DisposeZeroConf()
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
		private void StartHTTPServer()
		{
			// thread for the HTTP server.  its listen operation is blocking, so we can't start it before
			// we do any file scanning otherwise.
			Thread httpSrv = null;

			// Attempt to start the HTTP server thread
			try
			{
				httpServer = new HttpServer(Settings.Port);
				httpSrv = new Thread(new ThreadStart(httpServer.Listen));
				httpSrv.IsBackground = true;
				httpSrv.Start();
			}
			// Catch any socket exceptions which occur
			catch (System.Net.Sockets.SocketException e)
			{
				// If the address is in use, WaveBox (or another service) is probably bound to that port; error out
				// For another sockets exception, just print the message
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
					logger.Info("[WAVEBOX(1)] ERROR: Socket already in use.  Ensure that WaveBox is not already running.");
				else
					logger.Info("[WAVEBOX(2)] ERROR: " + e);

				// Quit with error return code
				Environment.Exit(-1);
			}
			// Catch any generic exception of non-Socket type
			catch (Exception e)
			{
				// Print the message, quit.
				logger.Info("[WAVEBOX(3)] ERROR: " + e);
				Environment.Exit(-1);
			}
		}

		public void Stop()
		{
			httpServer.Stop();
		}

		public void Restart()
		{
			Stop();
			StartHTTPServer();
		}
	}
}
