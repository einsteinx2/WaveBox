using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using WaveBox.Core;
using WaveBox.FolderScanning;
using WaveBox.TestFixtures;
using Xunit;

namespace WaveBox.Server.Tests {
    // Integration tests mutate process-global state (Injection's static service provider and the
    // WAVEBOX_ROOT/WAVEBOX_TEMP environment variables), so they must never run in parallel with
    // anything else in this assembly.
    [CollectionDefinition("Integration", DisableParallelization = true)]
    public class IntegrationCollection {
    }

    /// <summary>
    /// Boots a complete WaveBox backend against a throwaway root directory: real DI container
    /// (CoreModule + ServerModule), real SQLite databases copied from the bundled templates.
    /// Create one per test (xUnit constructs the test class per test method), which also gives
    /// every test fresh repository singletons - UserRepository and SessionRepository load
    /// whole-table caches in their constructors.
    /// </summary>
    public sealed class IntegrationHarness : IDisposable {
        public TempWaveBoxRoot Root { get; }
        public ServiceProvider Provider { get; }

        public IntegrationHarness() {
            Root = new TempWaveBoxRoot();

            ServiceCollection services = new ServiceCollection();
            services.AddWaveBoxCore();
            services.AddWaveBoxServer();
            Provider = services.BuildServiceProvider();

            Injection.Initialize(Provider);

            // Copy the bundled template databases into the fresh root and run schema upgrades.
            // This must happen before any repository is resolved: repository constructors and
            // even ORM write helpers (ExecuteLogged locks DbBackupLock, writes the query log)
            // go through Injection.Get<IDatabase>().
            Injection.Get<IDatabase>().DatabaseSetup();
        }

        /// <summary>
        /// Creates a media folder under the root containing one Mp3Fixture file per title, seeds
        /// wavebox.conf pointing at it, loads settings (which registers the media folder), and
        /// runs a full synchronous folder scan. Returns the media folder path.
        /// </summary>
        public string SeedMediaFolder(params string[] titles) {
            if (titles == null || titles.Length == 0) {
                titles = new string[] { "Test Song" };
            }

            string mediaDir = System.IO.Path.Combine(Root.Path, "media");
            Directory.CreateDirectory(mediaDir);
            foreach (string title in titles) {
                Mp3Fixture.Write(System.IO.Path.Combine(mediaDir, title + ".mp3"), title: title);
            }

            string template = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "wavebox.conf");
            WaveBoxConf.WriteSeeded(template, Root.Path, 6500, mediaDir);
            Injection.Get<IServerSettings>().SettingsSetup();

            new FolderScanOperation(mediaDir, 0).Start();

            return mediaDir;
        }

        public void Dispose() {
            // Poison accidental cross-test service use: Injection.Get now throws
            Injection.Initialize(null);
            Provider.Dispose();
            Root.Dispose();
        }
    }
}
