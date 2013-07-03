using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.Hosting;
using Ninject;
using Owin;
using WaveBox.Core.Injected;
using WaveBox.Service;
using WaveBox.Service.Services.DeviceSync;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	public class DeviceSyncService : IService
	{
		public string Name { get { return "devicesync"; } set { } }

		public bool Required { get { return false; } set { } }

		public bool Running { get; set; }

		public DeviceSyncService()
		{
		}

		public bool Start()
		{
			// Start the SignalR server for real time device state syncing
			try
			{
				WebApplication.Start<DeviceSyncStartup>("http://localhost:" + Injection.Kernel.Get<IServerSettings>().WsPort + "/");
			}
			catch (Exception e)
			{
				logger.Error("Could not start SignalR DeviceSync service");
				logger.Error(e);
				return false;
			}

			return true;
		}

		public bool Stop()
		{
			// todo: Actually find out how to stop SignalR
			return true;
		}
	}
}
