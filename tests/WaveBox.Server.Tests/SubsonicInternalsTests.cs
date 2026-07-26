using System;
using WaveBox.Subsonic;
using WaveBox.Subsonic.Handlers;
using WaveBox.Transcoding;
using Xunit;

namespace WaveBox.Server.Tests {
    public class SubsonicInternalsTests {
        [Theory]
        [InlineData("/ping.view", "ping")]
        [InlineData("/ping", "ping")]
        [InlineData("ping.view", "ping")]
        [InlineData("/ping.VIEW", "ping")]
        [InlineData("/getLicense.view", "getLicense")]
        [InlineData("//ping//", "ping")]
        public void EndpointNameStripsSlashesAndViewSuffix(string path, string expected) {
            Assert.Equal(expected, SubsonicDispatcher.EndpointName(path));
        }

        [Fact]
        public void EndpointNamePreservesInteriorCasing() {
            // Casing is normalized later at dispatch time, not here
            Assert.Equal("GetLicense", SubsonicDispatcher.EndpointName("/GetLicense.view"));
        }

        [Fact]
        public void EndpointNameNullOrEmptyReturnsNull() {
            Assert.Null(SubsonicDispatcher.EndpointName(null));
            Assert.Null(SubsonicDispatcher.EndpointName(""));
        }

        [Fact]
        public void EndpointNameOnlySlashesReturnsEmpty() {
            // Trim('/') of "/" leaves the empty string, which is returned as-is
            Assert.Equal("", SubsonicDispatcher.EndpointName("/"));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("beatles", "beatles")]
        [InlineData("  beatles  ", "beatles")]
        [InlineData("\"beatles\"", "beatles")]
        [InlineData("beatles*", "beatles")]
        [InlineData("\"beatles*\"", "beatles")]
        [InlineData("*", "")]
        [InlineData("", "")]
        public void CleanQueryTrimsWhitespaceQuotesAndWildcard(string input, string expected) {
            Assert.Equal(expected, SubsonicSearchHandlers.CleanQuery(input));
        }

        [Fact]
        public void CleanQueryTrimsQuotesBeforeTrailingWildcard() {
            // Pins the operation order Trim() -> Trim('"') -> TrimEnd('*'): a quote after the
            // wildcard survives the quote-trim pass since '*' is then the last character
            Assert.Equal("beatles\"", SubsonicSearchHandlers.CleanQuery("\"beatles\"*"));
        }

        [Theory]
        [InlineData(null, TranscodeType.MP3)]
        [InlineData("mp3", TranscodeType.MP3)]
        [InlineData("MP3", TranscodeType.MP3)]
        [InlineData("aac", TranscodeType.AAC)]
        [InlineData("m4a", TranscodeType.AAC)]
        [InlineData("mp4", TranscodeType.AAC)]
        [InlineData("ogg", TranscodeType.OGG)]
        [InlineData("OGG", TranscodeType.OGG)]
        [InlineData("oga", TranscodeType.OGG)]
        [InlineData("opus", TranscodeType.OPUS)]
        [InlineData("flac", TranscodeType.MP3)]
        [InlineData("raw", TranscodeType.MP3)]
        public void TranscodeTypeForFormatMapsKnownSuffixes(string format, TranscodeType expected) {
            Assert.Equal(expected, SubsonicMediaHandlers.TranscodeTypeForFormat(format));
        }
    }
}
