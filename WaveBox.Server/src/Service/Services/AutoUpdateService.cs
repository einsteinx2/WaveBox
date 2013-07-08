using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using WaveBox.Core.Extensions;
using WaveBox.OperationQueue;
using WaveBox.Service;

namespace WaveBox.Service.Services
{
	public class AutoUpdateService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "autoupdate"; } set { } }

		public bool Required { get { return false; } set { } }

		public bool Running { get; set; }

		// Hard coded release update check URL for now
		private const string ReleaseUpdateCheckUrl = "http://waveboxapp.com/release_updates.json";

		// Hard coded beta update check URL for now
		private const string BetaUpdateCheckUrl = "http://waveboxapp.com/beta_updates.json";

		// Hard coded check interval of 1 day for now
		private const int dueTime = 86400000;

		private string updateJson;
		public string UpdateJson { get { return updateJson; } }

		private bool isUpdateAvailable;
		public bool IsUpdateAvailable { get { return isUpdateAvailable; } }

		private List<UpdateInfo> updates;
		public List<UpdateInfo> Updates { get { return updates; } }

		private Timer timer;

		private WebClient client;

		public bool Start()
		{
			this.Stop();
			this.CheckForUpdate(null);

			return true;
		}

		public bool Stop()
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

			return true;
		}

		private void CheckForUpdate(object state)
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
				this.ParseJson();

				// Schedule another check
				timer = new Timer(CheckForUpdate, null, dueTime, Timeout.Infinite);
			});

			// Start the download
			client.DownloadStringAsync(new Uri(BetaUpdateCheckUrl));
		}

		private void ParseJson()
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
