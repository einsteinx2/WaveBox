using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.Model {
    public class ServerSettingsData {
        [JsonPropertyName("port")]
        public short Port { get; set; }

        [JsonPropertyName("wsPort")]
        public short WsPort { get; set; }

        [JsonPropertyName("theme")]
        public string Theme { get; set; }

        [JsonPropertyName("mediaFolders")]
        public IList<string> MediaFolders { get; set; }

        [JsonPropertyName("podcastFolder")]
        public string PodcastFolder { get; set; }

        [JsonPropertyName("podcastCheckInterval")]
        public int PodcastCheckInterval { get; set; }

        [JsonPropertyName("sessionTimeout")]
        public int SessionTimeout { get; set; }

        [JsonPropertyName("prettyJson")]
        public bool PrettyJson { get; set; }

        [JsonPropertyName("folderArtNames")]
        public IList<string> FolderArtNames { get; set; }

        [JsonPropertyName("crashReportEnable")]
        public bool CrashReportEnable { get; set; }

        [JsonPropertyName("services")]
        public IList<string> Services { get; set; }
    }
}
