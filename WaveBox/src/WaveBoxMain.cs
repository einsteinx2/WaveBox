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
using System.Net;
using System.Net.Sockets;

namespace WaveBox
{
	class WaveBoxMain
	{
		// Logger
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Nat
		public Nat Nat;

		// ZeroConf
		public RegisterService ZeroConfService { get; set; }

		// Server GUID and URL, for publishing
		public string ServerGuid { get; set; }
		public string ServerUrl { get; set; }

		// HTTP server, which serves up the API
		private HttpServer httpServer;

		/// <summary>
		/// Detects WaveBox's root directory, for storing per-user configuration
		/// </summary>
		public static string RootPath()
		{
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
		/// ServerSetup is used to generate a GUID which can be associated with the URL forwarding service, to 
		/// uniquely map an instance of WaveBox
		/// </summary>
		private void ServerSetup()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				// Grab server GUID and URL from the database
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
				logger.Error("exception loading server info", e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			// If it doesn't exist, generate a new one
			if ((object)ServerGuid == null)
			{
				// Generate the GUID
				Guid guid = Guid.NewGuid();
				ServerGuid = guid.ToString();

				// Store the GUID in the database
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
				catch (Exception e)
				{
					logger.Error("exception saving guid", e);
					ServerGuid = null;
				}
				finally
				{
					Database.Close(conn, null);
				}
			}
		}

		/// <summary>
		/// Return the IP address of the local adapter which WaveBox is running on
		/// </summary>
		private static IPAddress LocalIPAddress()
		{
			// If the network isn't available, IP will be null
			if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			{
				return null;
			}
			
			// Return host's IP address
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			
			return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
		}

		/// <summary>
		/// Register URL registers this instance of WaveBox with the URL forwarding service
		/// </summary>
		public void RegisterUrl()
		{
			if ((object)ServerUrl != null)
			{
				// Compose the registration request URL
				string urlString = "http://register.wavebox.es" + 
					"?host=" + Uri.EscapeUriString(ServerUrl) + 
					"&serverId=" + Uri.EscapeUriString(ServerGuid) + 
					"&port=" + Settings.Port + 
					"&isSecure=0" + 
					"&localIp=" + LocalIPAddress().ToString();

				if (logger.IsInfoEnabled) logger.Info("Registering URL: " + urlString);

				// Perform registration with registration server
				WebClient client = new WebClient();
				client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(RegisterUrlCompleted);
				client.DownloadDataAsync(new Uri(urlString));
			}
		}

		/// <summary>
		/// Handler for determining success and failure of server registration
		/// </summary>
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
			if (logger.IsInfoEnabled) logger.Info("Initializing WaveBox on " + WaveBoxService.Platform + " platform...");

			// Initialize ImageMagick
			ImageMagickInterop.WandGenesis();

			// Create directory for WaveBox's root path, if it doesn't exist
			string rootDir = RootPath();
			if (!Directory.Exists(rootDir))
			{
				Directory.CreateDirectory(rootDir);
			}

			// Perform initial setup of Settings, Database
			Database.DatabaseSetup();
			Settings.SettingsSetup();

			// Start NAT routing
			this.Nat = new Nat();
			this.Nat.Start();

			// Register server with registration service
			ServerSetup();
			RegisterUrl();

			// Start the HTTP server
			StartHTTPServer();

			// Start ZeroConf (broken as of 12/6/12)
			//PublishZeroConf();

			// Start transcode manager
			TranscodeManager.Instance.Setup();

			// Temporary: create test user
			User.CreateUser("test", "test");

			// Start file manager, calculate time it takes to run.
			if (logger.IsInfoEnabled) logger.Info("Scanning media directories...");
			FileManager.Instance.Setup();

			// Start podcast download queue
			PodcastManagement.DownloadQueue.FeedChecks.queueOperation(new FeedCheckOperation(0));
			PodcastManagement.DownloadQueue.FeedChecks.startScanQueue();

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			//Thread.Sleep(Timeout.Infinite);

			return;
		}

		/// <summary>
		/// Publish ZeroConf, so that WaveBox may advertise itself using mDNS to capable devices
		/// </summary>
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
					record.Add("URL", "http://something.wavebox.es");
					ZeroConfService.TxtRecord = record;
					
					ZeroConfService.Register();
				}
				catch (Exception e)
				{
					logger.Error(e);
					DisposeZeroConf();
				}
			}
		}

		/// <summary>
		/// Dispose of ZeroConf publisher
		/// </summary>
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
			// Thread for HTTP server run
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
				{
					logger.Error("Socket already in use.  Ensure that WaveBox is not already running.");
				}
				else
				{
					logger.Error(e);
				}

				// Quit with error return code
				Environment.Exit(-1);
			}
			// Catch any generic exception of non-Socket type
			catch (Exception e)
			{
				// Print the message, quit.
				logger.Error(e);
				Environment.Exit(-1);
			}
		}

		/// <summary>
		/// Stop the WaveBox main
		/// </summary>
		public void Stop()
		{
			httpServer.Stop();

			// Dispose of ImageMagick
			ImageMagickInterop.WandTerminus();
		}

		/// <summary>
		/// Restart the WaveBox main
		/// </summary>
		public void Restart()
		{
			Stop();
			StartHTTPServer();
		}
	}
}
