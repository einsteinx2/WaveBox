using System;
using System.IO;

namespace WaveBox.TestFixtures {
    /// <summary>
    /// Points WAVEBOX_ROOT / WAVEBOX_TEMP at a unique temp directory so the server-side code writes
    /// its config, databases, and transcode temp files there instead of the real user locations.
    /// Environment variables are process-global, so tests using this must run serialized (the
    /// "Integration" xUnit collection).
    /// </summary>
    public sealed class TempWaveBoxRoot : IDisposable {
        private readonly string previousRoot;
        private readonly string previousTemp;

        public string Path { get; }
        public string TempPath { get; }

        public TempWaveBoxRoot() {
            Path = Directory.CreateTempSubdirectory("wavebox-test-").FullName;
            TempPath = System.IO.Path.Combine(Path, "temp");
            Directory.CreateDirectory(TempPath);

            previousRoot = Environment.GetEnvironmentVariable("WAVEBOX_ROOT");
            previousTemp = Environment.GetEnvironmentVariable("WAVEBOX_TEMP");
            Environment.SetEnvironmentVariable("WAVEBOX_ROOT", Path);
            Environment.SetEnvironmentVariable("WAVEBOX_TEMP", TempPath);
        }

        public void Dispose() {
            Environment.SetEnvironmentVariable("WAVEBOX_ROOT", previousRoot);
            Environment.SetEnvironmentVariable("WAVEBOX_TEMP", previousTemp);
            try {
                Directory.Delete(Path, true);
            } catch (IOException) {
                // Best-effort cleanup; leaked temp dirs are harmless
            } catch (UnauthorizedAccessException) {
            }
        }
    }
}
