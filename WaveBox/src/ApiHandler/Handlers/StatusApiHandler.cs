using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text;
using WaveBox.Http;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using WaveBox.Transcoding;
using Newtonsoft.Json;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for StatusApiHandler class
		/// </summary>
		public StatusApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process is used to return a JSON object containing a variety of information about the host system
		/// which is running the WaveBox server
		/// </summary>
		public void Process()
		{
			logger.Info("[STATUS] OK!");
			try
			{
				// Allocate an array of various statistics about the running process
				IDictionary<string, string> status = new Dictionary<string, string>();

				// Gather data about WaveBox process
				global::System.Diagnostics.Process proc = global::System.Diagnostics.Process.GetCurrentProcess();

				// Get process ID
				status["pid"] = Convert.ToString(proc.Id);
				// Get last update time in UNIX format for status
				status["updated"] = StatusApiHandlerExtension.UnixTime(DateTime.Now).ToString();
				// Get WaveBox version, currently in the format 'wavebox-builddate-git' (change to true version later,
				// something like 'wavebox-1.0.0-alpha'
				status["version"] = "wavebox-" + WaveBoxService.BuildDate.ToString("yyyyMMdd") + "-git";
				// Get build date
				status["buildDate"] = WaveBoxService.BuildDate.ToString("MMMM dd, yyyy");
				// Get host platform
				status["platform"] = WaveBoxService.Platform;
				// Get current CPU usage
				status["cpu"] = StatusApiHandlerExtension.GetCPUUsage();
				// Get current memory usage in MB
				status["memory"] = Convert.ToString((((double)proc.WorkingSet64 / 1024) / 1024) + "MB");
				// Get peak memory usage in MB
				status["peakMemory"] = Convert.ToString((((double)proc.PeakWorkingSet64 / 1024) / 1024) + "MB");
				// Get list of media types WaveBox can index and serve
				status["mediaTypes"] = StatusApiHandlerExtension.GetMediaTypes();
				// Get list of transcoders available
				status["transcoders"] = StatusApiHandlerExtension.GetTranscoders();
				// Get last query log ID
				status["lastQueryLogId"] = Database.LastQueryLogId().ToString();

				string json = JsonConvert.SerializeObject(new StatusResponse(null, status), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				logger.Error("[STATUS(1)] ERROR: " + e);
			}
		}

		private class StatusResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("status")]
			public IDictionary<string, string> Status { get; set; }

			public StatusResponse(string error, IDictionary<string, string> status)
			{
				Error = error;
				Status = status;
			}
		}
	}

	// Utility code to aid in collection of system metrics
	class StatusApiHandlerExtension
	{
		// Borrowed from http://stackoverflow.com/questions/278071/how-to-get-the-cpu-usage-in-c
		/// <summary>
		/// Returns a string containing the CPU usage of WaveBox at this instant in time
		/// </summary>
		public static string GetCPUUsage()
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

			return usage + "%";
		}

		/// <summary>
		/// Grabs a list of valid file types for media files from the enumerator in Wavebox.DataModel.Model.FileType
		/// </summary>
		public static string GetMediaTypes()
		{
			var fileTypes = Enum.GetValues(typeof(FileType));
			Array.Sort(fileTypes);
			int i = 2;
			string types = "";

			foreach (FileType f in fileTypes)
			{
				if (f != FileType.Unknown)
				{
					if (i == fileTypes.Length)
					{
						types += f.ToString();
					}
					else
					{
						types += f.ToString() + ", ";
					}
				}

				i++;
			}

			return types;
		}

		/// <summary>
		/// Grabs a list of valid transcoder types from the enumerator in Wavebox.Transcoding.TranscoderType
		/// </summary>
		public static string GetTranscoders()
		{
			var transcoders = Enum.GetValues(typeof(TranscodeType));
			int i = 1;
			string trans = "";

			foreach (TranscodeType t in transcoders)
			{
				if (i == transcoders.Length)
				{
					trans += t.ToString();
				}
				else
				{
					trans += t.ToString() + ", ";
				}

				i++;
			}

			return trans;
		}

		/// <summary>
		/// Converts DateTime object to its UNIX timestamp equivalent
		/// </summary>
		public static double UnixTime(DateTime dt)
		{
			return (dt - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
		}
	}
}
