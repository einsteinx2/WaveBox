using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using WaveBox.Core.Injected;
using Ninject;

namespace WaveBox.Static
{
	public static class DynamicDns
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Return the IP address of the local adapter which WaveBox is running on
		/// </summary>
		public static IPAddress LocalIPAddress()
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
		public static void RegisterUrl(string serverUrl, string serverGuid)
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
		public static void RegisterUrlCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			// Do nothing for now, check for success and handle failures later
		}
	}
}

