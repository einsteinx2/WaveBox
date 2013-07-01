using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	class CronService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "Cron"; } set { } }

		private int Port { get { return Injection.Kernel.Get<IServerSettings>().Port; } set { } }

		public CronService()
		{
		}

		public bool Start()
		{
			return true;
		}

		public bool Stop()
		{
			return true;
		}
	}
}
