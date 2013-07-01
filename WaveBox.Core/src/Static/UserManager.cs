using System;
using System.Collections.Generic;
using WaveBox.Core.Extensions;
using WaveBox.Model;
using WaveBox.OperationQueue;

namespace WaveBox.Static
{
	public static class UserManager
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static DelayedOperationQueue purgeQueue;

		public static void Setup()
		{
			purgeQueue = new DelayedOperationQueue();
			purgeQueue.startQueue();
			purgeQueue.queueOperation(new PurgeOperation(0));
		}

		private class PurgeOperation : AbstractOperation
		{
			public override string OperationType { get { return String.Format("PurgeOperation: {0}", DateTime.Now.ToUniversalUnixTimestamp()); } }

			public PurgeOperation(int delayMilliSeconds) : base(delayMilliSeconds)
			{
			}

			public override void Start()
			{
				// Delete the expired users
				IList<User> users = User.ExpiredUsers();
				foreach (User user in users)
				{
					if (logger.IsInfoEnabled) logger.Info("Deleting expired user " + user.UserName + "  expired at " + user.DeleteTime);
					user.Delete();
				}

				// Schedule another check in 30 minutes
				purgeQueue.queueOperation(new PurgeOperation(60 * 30 * 1000));
			}
		}
	}
}
