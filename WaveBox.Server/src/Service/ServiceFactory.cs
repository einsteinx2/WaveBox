using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Service.Services;

namespace WaveBox.Service
{
	class ServiceFactory
	{
		/// <summary>
		/// Create a IService object which will be managed by ServiceManager
		/// <summary>
		public static IService CreateService(string service)
		{
			switch (service)
			{
				case "autoupdate":
					return new AutoUpdateService();
				case "cron":
					return new CronService();
				case "devicesync":
					return new DeviceSyncService();
				case "dynamicdns":
					return new DynamicDnsService();
				case "filemanager":
					return new FileManagerService();
				case "http":
					return new HttpService();
				case "jukebox":
					return new JukeboxService();
				case "nat":
					return new NatService();
				case "transcode":
					return new TranscodeService();
				case "zeroconf":
					return new ZeroConfService();
				default:
					return null;
			}
		}
	}
}
