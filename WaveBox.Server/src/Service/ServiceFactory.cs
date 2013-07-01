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
				case "cron":
					return new CronService();
				case "http":
					return new HttpService();
				case "nat":
					return new NatService();
				default:
					return null;
			}
		}
	}
}
