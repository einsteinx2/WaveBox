using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WaveBox.Model
{
	public class ServerSettingsData
	{
		[JsonProperty("port")]
		public short Port { get; set; }

		[JsonProperty("wsPort")]
		public short WsPort { get; set; }

		[JsonProperty("crashReportEnable")]
		public bool CrashReportEnable { get; set; }

		[JsonProperty("natEnable")]
		public bool NatEnable { get; set; }

		[JsonProperty("mediaFolders")]
		public List<string> MediaFolders { get; set; }

		[JsonProperty("podcastFolder")]
		public string PodcastFolder { get; set; }

		[JsonProperty("podcastCheckInterval")]
		public int PodcastCheckInterval { get; set; }

		[JsonProperty("sessionTimeout")]
		public int SessionTimeout { get; set; }

		[JsonProperty("prettyJson")]
		public bool PrettyJson { get; set; }

		[JsonProperty("folderArtNames")]
		public List<string> FolderArtNames { get; set; }
	}
}

