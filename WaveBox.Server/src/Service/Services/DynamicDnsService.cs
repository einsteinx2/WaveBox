using System;
using System.Data;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Injected;
using WaveBox.Service;
using WaveBox.Static;
using Ninject;

namespace WaveBox.Service.Services
{
	public class DynamicDnsService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Service name
		public string Name { get { return "dynamicdns"; } set { } }

		// Server GUID and URL for Dynamic DNS
		public string ServerGuid { get; set; }
		public string ServerUrl { get; set; }

		public DynamicDnsService()
		{
		}

		public bool Start()
		{
			this.ServerSetup();
			this.RegisterUrl(ServerUrl, ServerGuid);

			return true;
		}

		public bool Stop()
		{
			ServerUrl = null;
			ServerGuid = null;

			return true;
		}

		/// <summary>
		/// Return the IP address of the local adapter which WaveBox is running on
		/// </summary>
		public IPAddress LocalIPAddress()
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
		public void RegisterUrl(string serverUrl, string serverGuid)
		{
			if ((object)serverUrl != null)
			{
				// Compose the registration request URL
				string urlString = "http://register.wavebox.es" + 
					"?host=" + Uri.EscapeUriString(serverUrl) + 
					"&serverId=" + Uri.EscapeUriString(serverGuid) + 
					"&port=" + Injection.Kernel.Get<IServerSettings>().Port + 
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
		public void RegisterUrlCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			// Do nothing for now, check for success and handle failures later
		}

		/// <summary>
		/// ServerSetup is used to generate a GUID which can be associated with the URL forwarding service, to 
		/// uniquely map an instance of WaveBox
		/// </summary>
		private void ServerSetup()
		{
			ISQLiteConnection conn = null;
			try
			{
				// Grab server GUID and URL from the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				ServerGuid = conn.ExecuteScalar<string>("SELECT Guid FROM Server");
				ServerUrl = conn.ExecuteScalar<string>("SELECT Url FROM Server");
			}
			catch (Exception e)
			{
				logger.Error("Exception loading server info", e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					int affected = conn.Execute("INSERT INTO Server (Guid) VALUES (?)", ServerGuid);

					if (affected == 0)
					{
						ServerGuid = null;
					}
				}
				catch (Exception e)
				{
					logger.Error("Exception saving guid", e);
					ServerGuid = null;
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}
			}
		}
	}
}
