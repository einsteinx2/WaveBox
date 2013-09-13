﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Service;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;
using WaveBox.Transcoding;
using WaveBox;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.ApiResponse;
using WaveBox.Core;
using WaveBox.Core.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class StatusApiCache
	{
		public IDictionary<string, object> Cache = new Dictionary<string, object>();

		public long? LastQueryId { get; set; }
	}

	class StatusApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "status"; } }

		// Status API cache
		private static StatusApiCache statusCache = new StatusApiCache();
		public static StatusApiCache StatusCache { get { return statusCache; } }

		/// <summary>
		/// Process is used to return a JSON object containing a variety of information about the host system
		/// which is running the WaveBox server
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			try
			{
				// Allocate an array of various statistics about the running process
				IDictionary<string, object> status = new Dictionary<string, object>();

				// Gather data about WaveBox process
				global::System.Diagnostics.Process proc = global::System.Diagnostics.Process.GetCurrentProcess();

				// Get current UNIX time
				long unixTime = DateTime.Now.ToUniversalUnixTimestamp();

				// Get current query log ID
				long queryLogId = Injection.Kernel.Get<IDatabase>().LastQueryLogId;

				// Get auto updater instance, if available
				AutoUpdateService autoUpdater = (AutoUpdateService)ServiceManager.GetInstance("autoupdate");

				// Get process ID
				status["pid"] = proc.Id;
				// Get uptime of WaveBox instance
				status["uptime"] = unixTime - WaveBoxService.StartTime.ToUniversalUnixTimestamp();
				// Get last update time in UNIX format for status
				status["updated"] = unixTime;
				// Get hostname of machine
				status["hostname"] = System.Environment.MachineName;
				// Get WaveBox version
				status["version"] = WaveBoxService.BuildVersion;
				// Get build date
				status["buildDate"] = WaveBoxService.BuildDate.ToString("MMMM dd, yyyy");
				// Get host platform
				status["platform"] = WaveBoxService.Platform;
				// Get current CPU usage
				status["cpuPercent"] = CpuUsage();
				// Get current memory usage in MB
				status["memoryMb"] = (float)proc.WorkingSet64 / 1024f / 1024f;
				// Get peak memory usage in MB
				status["peakMemoryMb"] = (float)proc.PeakWorkingSet64 / 1024f / 1024f;
				// Get list of media types WaveBox can index and serve (removing "Unknown")
				status["mediaTypes"] = Enum.GetNames(typeof(FileType)).Where(x => x != "Unknown").ToList();
				// Get list of transcoders available
				status["transcoders"] = Enum.GetNames(typeof(TranscodeType)).ToList();
				// Get list of services
				status["services"] = ServiceManager.GetServices();
				// Get last query log ID
				status["lastQueryLogId"] = queryLogId;
				// If auto updater running...
				if (autoUpdater != null)
				{
					// Get whether an update is available or not
					status["isUpdateAvailable"] = autoUpdater.IsUpdateAvailable;
					// Get the list of updates for display to the user
					status["updateList"] = autoUpdater.Updates;
				}

				// Call for extended status, which uses some database intensive calls
				if (uri.Parameters.ContainsKey("extended"))
				{
					if (uri.Parameters["extended"].IsTrue())
					{
						// Check if any destructive queries have been performed since the last cache
						if ((statusCache.LastQueryId == null) || (queryLogId > statusCache.LastQueryId))
						{
							// Update to the latest query log ID
							statusCache.LastQueryId = queryLogId;

							logger.IfInfo("Gathering extended status metrics from database");

							// Get count of artists
							statusCache.Cache["artistCount"] = Injection.Kernel.Get<IArtistRepository>().CountArtists();
							// Get count of albums
							statusCache.Cache["albumCount"] = Injection.Kernel.Get<IAlbumRepository>().CountAlbums();
							// Get count of songs
							statusCache.Cache["songCount"] = Injection.Kernel.Get<ISongRepository>().CountSongs();
							// Get count of videos
							statusCache.Cache["videoCount"] = Injection.Kernel.Get<IVideoRepository>().CountVideos();
							// Get total file size of songs (bytes)
							statusCache.Cache["songFileSize"] = Injection.Kernel.Get<ISongRepository>().TotalSongSize();
							// Get total file size of videos (bytes)
							statusCache.Cache["videoFileSize"] = Injection.Kernel.Get<IVideoRepository>().TotalVideoSize();
							// Get total song duration
							statusCache.Cache["songDuration"] = Injection.Kernel.Get<ISongRepository>().TotalSongDuration();
							// Get total video duration
							statusCache.Cache["videoDuration"] = Injection.Kernel.Get<IVideoRepository>().TotalVideoDuration();

							logger.IfInfo("Metric gathering complete, cached results!");
						}

						// Append cached status dictionary to status
						status = status.Concat(statusCache.Cache).ToDictionary(x => x.Key, x => x.Value);
					}
				}

				// Return all status
				processor.WriteJson(new StatusResponse(null, status));
				return;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			// Return error
			processor.WriteJson(new StatusResponse("Could not retrieve server status", null));
			return;
		}

		// Borrowed from http://stackoverflow.com/questions/278071/how-to-get-the-cpu-usage-in-c
		/// <summary>
		/// Returns a string containing the CPU usage of WaveBox at this instant in time
		/// </summary>
		private float CpuUsage()
		{
			PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			cpuCounter.NextValue();
			Thread.Sleep(10);
			float usage = cpuCounter.NextValue();

			// If CPU usage is negative (ie, sudden drop in usage between cpuCounter.NextValue()), just return 0%
			if (usage < 0.00)
			{
				usage = 0.0f;
			}

			return usage;
		}
	}
}
