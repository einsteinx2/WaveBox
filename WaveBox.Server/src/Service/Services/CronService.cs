using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Service.Services.Cron;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	class CronService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "cron"; } set { } }

		public bool Required { get { return true; } set { } }

		public bool Running { get; set; }

		public CronService()
		{
		}

		public bool Start()
		{
			// FOR NOW: Use the pre-existing delayed operation queues.  Once more work has been done, cron will
			// run a unified timer and spin each of these off as needed
			logger.Info("Scheduling and starting all cronjobs...");

			// Start podcast download queue
			DownloadQueue.FeedChecks.queueOperation(new FeedCheckOperation(0));
			DownloadQueue.FeedChecks.startQueue();

			// Start user and session purge operation
			UserPurge.Queue.queueOperation(new UserPurgeOperation(0));
			UserPurge.Queue.startQueue();

			logger.Info("All cronjobs started!");

			return true;
		}

		public bool Stop()
		{
			// Stop all queues
			DownloadQueue.FeedChecks = null;
			UserPurge.Queue = null;

			return true;
		}
	}
}
