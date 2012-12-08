using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text;
using WaveBox.Http;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;
using WaveBox.DataModel.Model;
using WaveBox.Transcoding;
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

				// Get current build date
				DateTime buildDate = StatusApiHandlerExtension.GetBuildDate();

				// Get process ID
				status["pid"] = Convert.ToString(proc.Id);
				// Get WaveBox version, currently in the format 'wavebox-builddate-git' (change to true version later,
				// something like 'wavebox-1.0.0-alpha'
				status["version"] = "wavebox-" + buildDate.ToString("yyyyMMdd") + "-git";
				// Get build date
				status["buildDate"] = buildDate.ToString("MMMM dd, yyyy");
				// Get host platform
				status["platform"] = StatusApiHandlerExtension.GetPlatform();
				// Get current CPU usage
				status["cpu"] = StatusApiHandlerExtension.GetCPUUsage();
				// Get current memory usage in MB
				status["memory"] = Convert.ToString((((double)proc.WorkingSet64 / 1024) / 1024) + "MB");
				// Get peak memory usage in MB
				status["peakMemory"] = Convert.ToString((((double)proc.PeakWorkingSet64 / 1024) / 1024) + "MB");
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
			if(usage < 0.00)
			{
				usage = 0.0f;
			}

			return usage + "%";
		}

		// Borrowed from: http://stackoverflow.com/questions/1600962/displaying-the-build-date
		/// <summary>
		/// Returns a DateTime object containing the date on which WaveBox was compiled (good for nightly build names,
		/// as well as information on reporting issues which may occur later on.
		/// </summary>
		public static DateTime GetBuildDate()
		{
			// Read the PE header to get build date
		    string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
		    const int c_PeHeaderOffset = 60;
		    const int c_LinkerTimestampOffset = 8;
		    byte[] b = new byte[2048];
		    System.IO.Stream s = null;

		    try
		    {
		        s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
		        s.Read(b, 0, 2048);
		    }
		    finally
		    {
		        if (s != null)
		        {
					s.Close();
		        }
		    }

		    int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
		    int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
		    DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
		    dt = dt.AddSeconds(secondsSince1970);
		    dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
		    return dt;
		}

		/// <summary>
		/// Utilizes the DetectOS method from the Service module to determine our running platform
		/// </summary>
		public static string GetPlatform()
		{
			switch(WaveBoxService.DetectOS())
			{
				case WaveBoxService.OS.Windows:
					return "Windows";
				case WaveBoxService.OS.MacOSX:
					return "Mac OS X";
				case WaveBoxService.OS.Unix:
					return "UNIX/Linux";
				default:
					return "Unknown";
			}
		}

		/// <summary>
		/// Grabs a list of valid transcoder types from the enumerator in Wavebox.Transcoding.TranscoderType
		/// </summary>
		public static string GetTranscoders()
		{
			var transcoders = Enum.GetValues(typeof(TranscodeType));
			int i = 1;
			string trans = "";

			foreach(TranscodeType t in transcoders)
			{
				if(i == transcoders.Length)
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
	}
}
