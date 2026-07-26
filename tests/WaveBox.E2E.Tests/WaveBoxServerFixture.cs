using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WaveBox.TestFixtures;
using Xunit;

namespace WaveBox.E2E.Tests {
    /// <summary>
    /// Boots a real WaveBox server process once for the whole E2E collection: isolated root dir via
    /// WAVEBOX_ROOT, random free port, generated MP3 fixture as the media folder. The binary comes
    /// from WAVEBOX_E2E_BINARY (CI points this at the NativeAOT publish output) or falls back to the
    /// locally built server, which the E2E csproj guarantees is fresh via a build-only ProjectReference.
    /// </summary>
    public sealed class WaveBoxServerFixture : IAsyncLifetime {
        private Process serverProcess;
        private string workDir;
        private StreamWriter logWriter;
        private readonly object logLock = new object();

        public HttpClient Client { get; private set; }
        public int Port { get; private set; }
        public string Session { get; private set; }
        public string SongId { get; private set; }
        public string ServerLogPath { get; private set; }

        public static bool FfmpegPresent { get; } = DetectFfmpeg();

        public async ValueTask InitializeAsync() {
            string binary = LocateBinary();

            workDir = Directory.CreateTempSubdirectory("wavebox-e2e-").FullName;
            string mediaDir = Path.Combine(workDir, "media");
            string rootDir = Path.Combine(workDir, "root");
            string tempDir = Path.Combine(workDir, "temp");
            Directory.CreateDirectory(mediaDir);
            Directory.CreateDirectory(tempDir);

            Mp3Fixture.Write(Path.Combine(mediaDir, "test_song.mp3"));

            // Seed the conf before first boot (the old smoke-test.sh had to boot, patch, restart)
            Port = TcpPort.GetFree();
            string templatePath = Path.Combine(Path.GetDirectoryName(binary), "res", "wavebox.conf");
            if (!File.Exists(templatePath)) {
                throw new FileNotFoundException("Bundled conf template not found next to the server binary: " + templatePath);
            }
            WaveBoxConf.WriteSeeded(templatePath, rootDir, Port, mediaDir);

            ServerLogPath = Path.Combine(workDir, "server.log");
            logWriter = new StreamWriter(ServerLogPath) { AutoFlush = true };

            ProcessStartInfo startInfo = new ProcessStartInfo(binary) {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(binary)
            };
            startInfo.Environment["WAVEBOX_ROOT"] = rootDir;
            startInfo.Environment["WAVEBOX_TEMP"] = tempDir;

            serverProcess = new Process { StartInfo = startInfo };
            serverProcess.OutputDataReceived += (s, e) => WriteLog(e.Data);
            serverProcess.ErrorDataReceived += (s, e) => WriteLog(e.Data);
            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();

            Client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + Port + "/") };

            await WaitForServerReady();
            await LoginAndWaitForScan();
        }

        public async ValueTask DisposeAsync() {
            if (Client != null) {
                Client.Dispose();
            }
            if (serverProcess != null && !serverProcess.HasExited) {
                serverProcess.Kill(true);
                await serverProcess.WaitForExitAsync();
            }
            if (logWriter != null) {
                logWriter.Dispose();
            }
            if (workDir != null) {
                try {
                    Directory.Delete(workDir, true);
                } catch (IOException) {
                    // Best-effort cleanup
                } catch (UnauthorizedAccessException) {
                }
            }
        }

        private void WriteLog(string line) {
            if (line == null) {
                return;
            }
            lock (logLock) {
                logWriter.WriteLine(line);
            }
        }

        private async Task WaitForServerReady() {
            for (int i = 0; i < 120; i++) {
                if (serverProcess.HasExited) {
                    throw new InvalidOperationException("Server process exited early.\n" + LogTail());
                }
                try {
                    HttpResponseMessage response = await Client.GetAsync("api/status");
                    if (response.StatusCode > 0) {
                        return;
                    }
                } catch (HttpRequestException) {
                    // Not listening yet
                }
                await Task.Delay(500);
            }
            throw new TimeoutException("Server did not become ready on port " + Port + " within 60s.\n" + LogTail());
        }

        private async Task LoginAndWaitForScan() {
            string body = await Client.GetStringAsync("api/login?u=test&p=test");
            Session = JsonNode.Parse(body)["sessionId"]?.GetValue<string>();
            if (String.IsNullOrEmpty(Session)) {
                throw new InvalidOperationException("Login did not return a sessionId: " + body + "\n" + LogTail());
            }

            for (int i = 0; i < 60; i++) {
                string songsBody = await Client.GetStringAsync("api/songs?s=" + Session);
                JsonNode songs = JsonNode.Parse(songsBody)["songs"];
                if (songs is JsonArray array && array.Count > 0) {
                    SongId = array[0]["itemId"].ToString();
                    return;
                }
                await Task.Delay(1000);
            }
            throw new TimeoutException("Media scanner did not index the fixture song within 60s.\n" + LogTail());
        }

        public string LogTail(int lines = 40) {
            try {
                lock (logLock) {
                    logWriter.Flush();
                }
                string[] all = File.ReadAllLines(ServerLogPath);
                return "--- server log tail ---\n" + String.Join("\n", all.Skip(Math.Max(0, all.Length - lines)));
            } catch (IOException) {
                return "(server log unavailable)";
            }
        }

        private static string LocateBinary() {
            string fromEnv = Environment.GetEnvironmentVariable("WAVEBOX_E2E_BINARY");
            if (!String.IsNullOrEmpty(fromEnv)) {
                if (File.Exists(fromEnv)) {
                    return fromEnv;
                }
                if (File.Exists(fromEnv + ".exe")) {
                    return fromEnv + ".exe";
                }
                throw new FileNotFoundException("WAVEBOX_E2E_BINARY is set but no server binary exists at " + fromEnv);
            }

            string repoRoot = AssemblyMetadataValue("RepoRoot");
            string configuration = AssemblyMetadataValue("BuildConfiguration");
            string basePath = Path.Combine(repoRoot, "WaveBox.Server", "bin", configuration, "net10.0", "WaveBox.Server");
            if (File.Exists(basePath)) {
                return basePath;
            }
            if (File.Exists(basePath + ".exe")) {
                return basePath + ".exe";
            }
            throw new FileNotFoundException(
                "No server binary found. Either build it (dotnet build WaveBox.Server -c " + configuration +
                ") or set WAVEBOX_E2E_BINARY to a published binary. Looked at: " + basePath);
        }

        private static string AssemblyMetadataValue(string key) {
            AssemblyMetadataAttribute attribute = typeof(WaveBoxServerFixture).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == key);
            if (attribute == null || String.IsNullOrEmpty(attribute.Value)) {
                throw new InvalidOperationException("Assembly metadata '" + key + "' missing from test assembly");
            }
            return attribute.Value;
        }

        private static bool DetectFfmpeg() {
            try {
                ProcessStartInfo probe = new ProcessStartInfo("ffmpeg", "-version") {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = Process.Start(probe)) {
                    process.WaitForExit(10000);
                    return process.HasExited && process.ExitCode == 0;
                }
            } catch (Exception) {
                return false;
            }
        }
    }

    [CollectionDefinition("E2E")]
    public class E2ECollection : ICollectionFixture<WaveBoxServerFixture> {
    }
}
