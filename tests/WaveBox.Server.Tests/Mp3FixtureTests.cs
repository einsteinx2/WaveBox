using System;
using System.IO;
using WaveBox.TestFixtures;
using Xunit;

namespace WaveBox.Server.Tests {
    public class Mp3FixtureTests {
        [Fact]
        public void GeneratedMp3RoundTripsThroughTagLib() {
            string path = Path.Combine(Path.GetTempPath(), "wavebox-fixture-" + Guid.NewGuid().ToString("N") + ".mp3");
            try {
                Mp3Fixture.Write(path);

                using (TagLib.File file = TagLib.File.Create(path)) {
                    Assert.Equal("Test Song", file.Tag.Title);
                    Assert.Equal("Test Artist", file.Tag.FirstPerformer);
                    Assert.Equal("Test Album", file.Tag.Album);
                    Assert.Equal("Rock", file.Tag.FirstGenre);
                    Assert.True(file.Properties.Duration > TimeSpan.Zero);
                }
            } finally {
                File.Delete(path);
            }
        }

        [Fact]
        public void GeneratedMp3HonorsCustomTags() {
            string path = Path.Combine(Path.GetTempPath(), "wavebox-fixture-" + Guid.NewGuid().ToString("N") + ".mp3");
            try {
                Mp3Fixture.Write(path, title: "Other Song", artist: "Other Artist", album: "Other Album", genre: "Jazz", seconds: 2);

                using (TagLib.File file = TagLib.File.Create(path)) {
                    Assert.Equal("Other Song", file.Tag.Title);
                    Assert.Equal("Other Artist", file.Tag.FirstPerformer);
                    Assert.Equal("Other Album", file.Tag.Album);
                    Assert.Equal("Jazz", file.Tag.FirstGenre);
                }
            } finally {
                File.Delete(path);
            }
        }
    }
}
