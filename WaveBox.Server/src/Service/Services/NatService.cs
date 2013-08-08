using System;
using Mono.Nat;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Service;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	public enum NatStatus
	{
		NotInitialized = 0,
		WaitingForDevice = 1,
		DeviceFound = 2,
		PortForwardedSuccessfully = 3,
		PortForwardingFailed = 4
	}

	public class NatService : IService
	{
		// Logger
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "nat"; } set { } }

		public bool Required { get { return false; } set { } }

		public bool Running { get; set; }

		public NatStatus Status { get; set; }

		private INatDevice Device { get; set; }

		public NatService()
		{
		}

		public bool Start()
		{
			this.Status = NatStatus.WaitingForDevice;

			// Hook into the events so you know when a router has been detected or has gone offline
			NatUtility.DeviceFound += DeviceFound;
			NatUtility.DeviceLost += DeviceLost;
			NatUtility.UnhandledException += UnhandledException;

			// Start searching for upnp enabled routers
			NatUtility.StartDiscovery();

			return true;
		}

		public bool Stop()
		{
			if ((object)Device != null && Status == NatStatus.PortForwardedSuccessfully)
			{
				Device.DeletePortMap(new Mapping(Protocol.Tcp, Injection.Kernel.Get<IServerSettings>().Port, Injection.Kernel.Get<IServerSettings>().Port));
				Status = NatStatus.NotInitialized;
			}

			return true;
		}

		private void DeviceFound(object sender, DeviceEventArgs args)
		{
			logger.IfInfo("Device Found");

			this.Status = NatStatus.DeviceFound;

			// This is the upnp enabled router
			this.Device = args.Device;

			// Create a mapping to forward external port to local port
			try
			{
				Device.CreatePortMap(new Mapping(Protocol.Tcp, Injection.Kernel.Get<IServerSettings>().Port, Injection.Kernel.Get<IServerSettings>().Port));
				this.Status = NatStatus.PortForwardedSuccessfully;
			}
			catch (Exception e)
			{
				this.Status = NatStatus.PortForwardingFailed;
				logger.Error("Port mapping failed", e);
			}
		}

		private void DeviceLost(object sender, DeviceEventArgs args)
		{
			this.Status = NatStatus.PortForwardingFailed;

			INatDevice device = args.Device;

			logger.IfInfo("Device Lost");
			logger.IfInfo("Type: " + device.GetType().Name);
		}

		private void UnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			this.Status = NatStatus.PortForwardingFailed;

			logger.Error("Unhandled exception: " + args);
		}
	}
}
