using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.OperationQueue;

namespace WaveBox.SessionManagement
{
	/// <summary>
	/// Scrub all sessions which are out of date, using WaveBox settings
	/// </summary>
	public static class SessionScrub
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Create operation queue for the session scrubber
		public static DelayedOperationQueue Queue = new DelayedOperationQueue();

		/// <summary>
		/// Start session scrubbing operation
		/// </summary>
		public static void Start()
		{
			// Grab a list of all sessions
			var sessions = Session.AllSessions();

			// Grab the current UNIX time
			long unixTime = DateTime.Now.ToUniversalUnixTimestamp();

			// Purge any sessions which have not been updated in a predefined period of time
			foreach (Session s in sessions)
			{
				// Check current time and last update, purge if the diff is higher than SessionTimeout minutes
				if ((unixTime - Convert.ToInt32(s.UpdateTime)) >= (Settings.SessionTimeout * 60))
				{
					if (s.DeleteSession())
					{
						if (logger.IsInfoEnabled) logger.Info(String.Format("Purged session: [id: {0}, user: {1}]", s.RowId, s.UserId));
					}
					else
					{
						if (logger.IsInfoEnabled) logger.Info(String.Format("Failed to purge session: [id: {0}, user: {1}]", s.RowId, s.UserId));
					}
				}
			}
		}
	}
}
