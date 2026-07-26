using System;
using System.IO;
using WaveBox.Core;
using WaveBox.Static;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class ServerSettingsTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly IServerSettings settings;

        public ServerSettingsTests() {
            harness = new IntegrationHarness();
            settings = Injection.Get<IServerSettings>();
        }

        public void Dispose() {
            harness.Dispose();
        }

        [Fact]
        public void SettingsSetupSeedsConfFromTemplateAndParsesIt() {
            settings.SettingsSetup();

            Assert.True(File.Exists(Path.Combine(harness.Root.Path, "wavebox.conf")));
            Assert.Equal(6500, settings.Port);
            Assert.Equal("wave", settings.Theme);
            Assert.True(settings.PrettyJson);
            Assert.Equal(120, settings.SessionTimeout);
            Assert.Equal(4, settings.FolderArtNames.Count);
            Assert.Contains("cover.jpg", settings.FolderArtNames);
        }

        [Fact]
        public void ParseSettingsAcceptsCommentsAndTrailingCommas() {
            string conf = "// leading comment\n" +
                          "{\n" +
                          "  /* block comment */\n" +
                          "  \"port\": 7777,\n" +
                          "  \"theme\": \"dark\", // trailing comment\n" +
                          "  \"mediaFolders\": [\"/nonexistent\"],\n" +
                          "}\n";
            File.WriteAllText(Path.Combine(harness.Root.Path, "wavebox.conf"), conf);

            settings.Reload();

            Assert.Equal(7777, settings.Port);
            Assert.Equal("dark", settings.Theme);
            Assert.Equal(new[] { "/nonexistent" }, settings.MediaFolders);
        }

        [Fact]
        public void WriteSettingsRoundTripsLargePortThroughDisk() {
            settings.SettingsSetup();

            // Port is int (was short) - a port above Int16.MaxValue must survive the round trip
            Assert.True(settings.WriteSettings("{\"port\": 61735, \"prettyJson\": false}"));
            Assert.Equal(61735, settings.Port);

            settings.Reload();

            Assert.Equal(61735, settings.Port);
            Assert.False(settings.PrettyJson);
        }

        [Fact]
        public void WriteSettingsPersistsAcrossFreshInstances() {
            settings.SettingsSetup();
            Assert.True(settings.WriteSettings("{\"theme\": \"midnight\", \"sessionTimeout\": 45}"));

            ServerSettings fresh = new ServerSettings();
            fresh.Reload();

            Assert.Equal("midnight", fresh.Theme);
            Assert.Equal(45, fresh.SessionTimeout);
            Assert.Equal(6500, fresh.Port);
        }

        [Fact]
        public void WriteSettingsIgnoresMalformedValuesButAppliesValidOnes() {
            settings.SettingsSetup();

            // Pins actual behavior: each setting parses in its own try/catch, so a bad port is
            // silently dropped while the valid theme still applies (and the call reports change)
            Assert.True(settings.WriteSettings("{\"port\": \"not-a-number\", \"theme\": \"dark\"}"));

            Assert.Equal(6500, settings.Port);
            Assert.Equal("dark", settings.Theme);
        }

        [Fact]
        public void WriteSettingsRejectsInvalidJsonAndEmptyObjects() {
            settings.SettingsSetup();

            Assert.False(settings.WriteSettings("this is not json"));
            Assert.False(settings.WriteSettings("{}"));
            Assert.Equal(6500, settings.Port);
        }

        [Fact]
        public void WriteSettingsUpdatesMediaFolders() {
            settings.SettingsSetup();
            string folder = Path.Combine(harness.Root.Path, "music");
            Directory.CreateDirectory(folder);
            string escaped = folder.Replace("\\", "\\\\");

            Assert.True(settings.WriteSettings("{\"mediaFolders\": [\"" + escaped + "\"]}"));
            Assert.Equal(new[] { folder }, settings.MediaFolders);

            settings.Reload();
            Assert.Equal(new[] { folder }, settings.MediaFolders);
        }
    }
}
