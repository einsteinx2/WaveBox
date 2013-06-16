using System;
using WaveBox.OperationQueue;
using System.Threading;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Extensions;

namespace WaveBox.Static
{
	public static class AutoUpdater
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Hard coded release update check URL for now
		private const string ReleaseUpdateCheckUrl = "http://waveboxapp.com/release_updates.json";

		// Hard coded beta update check URL for now
		private const string BetaUpdateCheckUrl = "http://waveboxapp.com/beta_updates.json";

		// Hard coded check interval of 1 day for now
		private const int dueTime = 86400000;

		private static string updateJson;
		public static string UpdateJson { get { return updateJson; } }

		private static bool isUpdateAvailable;
		public static bool IsUpdateAvailable { get { return isUpdateAvailable; } }

		private static List<UpdateInfo> updates;
		public static List<UpdateInfo> Updates { get { return updates; } }

		private static Timer timer;

		private static WebClient client;

		public static void Start()
		{
			Stop();

			CheckForUpdate(null);
		}

		public static void Stop()
		{
			if ((object)client != null)
			{
				client.CancelAsync();
				client.Dispose();
				client = null;
			}

			if ((object)timer != null)
			{
				timer.Dispose();
				timer = null;
			}
		}

		private static void CheckForUpdate(object state)
		{
			if (logger.IsInfoEnabled) logger.Info("Checking for updates");

			// Cancel any existing connection
			if ((object)client != null)
			{
				client.CancelAsync();
			}
			client = new WebClient();

			// Setup the callback
			client.DownloadStringCompleted += new DownloadStringCompletedEventHandler((sender, args) => {
				// Parse the JSON and do something useful with it
				updateJson = args.Result;
				ParseJson();

				// Schedule another check
				timer = new Timer(CheckForUpdate, null, dueTime, Timeout.Infinite);
			});

			// Start the download
			client.DownloadStringAsync(new Uri(BetaUpdateCheckUrl));
		}

		private static void ParseJson()
		{
			if ((object)updateJson == null)
			{
				isUpdateAvailable = false;
				return;
			}

			try
			{
				List<UpdateInfo> updatesTemp = (List<UpdateInfo>)JsonConvert.DeserializeObject(updateJson, typeof(List<UpdateInfo>));
				if (updatesTemp.Count > 0)
				{
					// Check if an update is available
					if (WaveBoxService.BuildDate.ToUniversalUnixTimestamp() < updatesTemp[0].BuildTime)
					{
						isUpdateAvailable = true;
					}

					// Save the update list
					updates = updatesTemp;
				}
			}
			catch (Exception e)
			{
				logger.Error("Exception trying to check for updates : ", e);
			}
		}
	}

	public class UpdateInfo
	{
		public long BuildTime { get; set; }
		public string DisplayVersion { get; set; }
		public string ChangeLog { get; set; }
	}
}
