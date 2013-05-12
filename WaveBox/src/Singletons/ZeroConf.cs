using System;
using Mono.Zeroconf;

namespace WaveBox.Singletons
{
	public class ZeroConf
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static string Name { get { return System.Environment.MachineName; } }
		private const string RegType = "_wavebox._tcp";
		private const string ReplyDomain = "local.";
		private const string ServerUrlKey = "URL";

		// ZeroConf
		private static RegisterService ZeroConfService { get; set; }

		/// <summary>
		/// Publish ZeroConf, so that WaveBox may advertise itself using mDNS to capable devices
		/// </summary>
		public static void PublishZeroConf(string serverUrl, short port)
		{
			if ((object)serverUrl == null)
				return;

			// If we're already registered, dispose of it and create a new one
			if ((object)ZeroConfService != null)
			{
				DisposeZeroConf();
			}

			// Create and register the service
			try
			{
				ZeroConfService = new RegisterService();
				ZeroConfService.Name = Name;
				ZeroConfService.RegType = RegType;
				ZeroConfService.ReplyDomain = ReplyDomain;
				ZeroConfService.Port = (short)Settings.Port;

				TxtRecord record = new TxtRecord();
				record.Add(ServerUrlKey, serverUrl);
				ZeroConfService.TxtRecord = record;

				ZeroConfService.Register();
			}
			catch (Exception e)
			{
				logger.Error(e);
				DisposeZeroConf();
			}
		}

		/// <summary>
		/// Dispose of ZeroConf publisher
		/// </summary>
		public static void DisposeZeroConf()
		{
			if ((object)ZeroConfService != null)
			{
				ZeroConfService.Dispose();
				ZeroConfService = null;
			}
		}
	}
}

