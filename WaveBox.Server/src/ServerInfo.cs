using System;
using System.IO;

namespace WaveBox {
    // Static server metadata, formerly held on the WaveBoxService ServiceBase class
    public static class ServerInfo {
        // WaveBox temporary folder, for transcodes and such; WAVEBOX_TEMP overrides the shared
        // default so test runs (and concurrent instances) don't delete each other's files
        public static string TempFolder = Environment.GetEnvironmentVariable("WAVEBOX_TEMP") is string overrideTemp && overrideTemp.Length > 0
            ? overrideTemp
            : Path.Combine(Path.GetTempPath(), "wavebox");

        // Operating system enumeration
        public static ServerUtility.OS OS { get; set; }

        // Current version of WaveBox, from assembly
        public static string BuildVersion { get; set; }

        // Build date of WaveBox (for versioning, status metric)
        public static DateTime BuildDate { get; set; }

        // Start time, used to calculate uptime
        public static DateTime StartTime { get; set; }
    }
}
