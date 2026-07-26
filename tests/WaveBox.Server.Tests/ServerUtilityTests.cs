using System;
using System.IO;
using Xunit;

namespace WaveBox.Server.Tests {
    // RootPath tests mutate process-wide environment variables, so this collection must not
    // run in parallel with anything else
    [CollectionDefinition("EnvVars", DisableParallelization = true)]
    public class EnvVarsCollection {
    }

    [Collection("EnvVars")]
    public class ServerUtilityTests {
        [Fact]
        public void DetectOsReturnsCurrentPlatform() {
            ServerUtility.OS os = ServerUtility.DetectOS();

            if (OperatingSystem.IsWindows()) {
                Assert.Equal(ServerUtility.OS.Windows, os);
            } else if (OperatingSystem.IsMacOS()) {
                Assert.Equal(ServerUtility.OS.MacOSX, os);
            } else if (OperatingSystem.IsLinux()) {
                Assert.Equal(ServerUtility.OS.Linux, os);
            } else {
                Assert.NotEqual(ServerUtility.OS.Unknown, os);
            }
        }

        [Theory]
        [InlineData(ServerUtility.OS.Windows, "Windows")]
        [InlineData(ServerUtility.OS.MacOSX, "Mac OS X")]
        [InlineData(ServerUtility.OS.Linux, "Linux")]
        [InlineData(ServerUtility.OS.BSD, "BSD")]
        [InlineData(ServerUtility.OS.Solaris, "Solaris")]
        [InlineData(ServerUtility.OS.Unix, "Unix")]
        [InlineData(ServerUtility.OS.Unknown, "Unknown")]
        public void ToDescriptionMapsEveryValue(ServerUtility.OS os, string expected) {
            Assert.Equal(expected, os.ToDescription());
        }

        [Fact]
        public void RootPathHonorsEnvironmentOverride() {
            string original = Environment.GetEnvironmentVariable("WAVEBOX_ROOT");
            try {
                string overrideRoot = Path.Combine(Path.GetTempPath(), "wavebox-test-root");
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", overrideRoot);

                // A trailing separator is appended when missing
                Assert.Equal(overrideRoot + Path.DirectorySeparatorChar, ServerUtility.RootPath());
            } finally {
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", original);
            }
        }

        [Fact]
        public void RootPathKeepsExistingTrailingSeparator() {
            string original = Environment.GetEnvironmentVariable("WAVEBOX_ROOT");
            try {
                string overrideRoot = Path.Combine(Path.GetTempPath(), "wavebox-test-root") + Path.DirectorySeparatorChar;
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", overrideRoot);

                Assert.Equal(overrideRoot, ServerUtility.RootPath());
            } finally {
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", original);
            }
        }

        [Fact]
        public void RootPathDefaultEndsWithSeparator() {
            string original = Environment.GetEnvironmentVariable("WAVEBOX_ROOT");
            try {
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", null);

                string root = ServerUtility.RootPath();

                Assert.False(String.IsNullOrEmpty(root));
                Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), root);
            } finally {
                Environment.SetEnvironmentVariable("WAVEBOX_ROOT", original);
            }
        }
    }
}
