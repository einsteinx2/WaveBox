using System;
using Mono.Zeroconf;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Service;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	public class ZeroConfService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "zeroconf"; } set { } }

		public bool Required { get { return false; } set { } }

		public bool Running { get; set; }

		private string Hostname { get { return System.Environment.MachineName; } }
		private const string RegType = "_wavebox._tcp";
		private const string ReplyDomain = "local.";
		private const string ServerUrlKey = "URL";

		// ZeroConf
		private static RegisterService ZeroConf { get; set; }

		/// <summary>
		/// Publish ZeroConf, so that WaveBox may advertise itself using mDNS to capable devices
		/// </summary>
		public bool Start()
		{
			string serverUrl = ServerUtility.GetServerUrl();
			if ((object)serverUrl == null)
			{
				logger.Error("Could not start ZeroConf service, due to null ServerUrl");
				return false;
			}

			// If we're already registered, dispose of it and create a new one
			if ((object)ZeroConf != null)
			{
				this.Stop();
			}

			// Create and register the service
			try
			{
				ZeroConf = new RegisterService();
				ZeroConf.Name = Hostname;
				ZeroConf.RegType = RegType;
				ZeroConf.ReplyDomain = ReplyDomain;
				ZeroConf.Port = (short)Injection.Kernel.Get<IServerSettings>().Port;

				TxtRecord record = new TxtRecord();
				record.Add(ServerUrlKey, serverUrl);
				ZeroConf.TxtRecord = record;

				ZeroConf.Register();
			}
			catch (Exception e)
			{
				logger.Error(e);
				this.Stop();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Dispose of ZeroConf publisher
		/// </summary>
		public bool Stop()
		{
			if ((object)ZeroConf != null)
			{
				ZeroConf.Dispose();
				ZeroConf = null;
			}

			return true;
		}
	}
}
