using System;
using System.IO;

namespace WaveBox.TestFixtures {
    /// <summary>
    /// Seeds a wavebox.conf into a WaveBox root directory by patching the bundled template, so the
    /// server boots once with the right port and media folder (the old smoke-test.sh had to boot,
    /// patch, and restart because it let the server seed the default conf first).
    /// </summary>
    public static class WaveBoxConf {
        /// <param name="templatePath">Path to a pristine res/wavebox.conf (from the server's output or publish dir).</param>
        /// <param name="rootDir">WaveBox root directory to write wavebox.conf into.</param>
        /// <param name="port">Port to listen on.</param>
        /// <param name="mediaFolder">Media folder to scan.</param>
        public static void WriteSeeded(string templatePath, string rootDir, int port, string mediaFolder) {
            string conf = File.ReadAllText(templatePath);

            conf = conf.Replace("\"port\": 6500", "\"port\": " + port);

            // JSON string escaping for Windows backslashes
            string escaped = mediaFolder.Replace("\\", "\\\\").Replace("\"", "\\\"");
            conf = conf.Replace("\"/srv/your/media/here\"", "\"" + escaped + "\"");

            // Don't touch the host's router with UPnP port mappings during tests
            conf = conf.Replace("\"nat\"", "\"!nat\"");

            Directory.CreateDirectory(rootDir);
            File.WriteAllText(Path.Combine(rootDir, "wavebox.conf"), conf);
        }
    }
}
