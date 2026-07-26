using System;
using WaveBox.Core.Model;
using WaveBox.Transcoding;
using Xunit;

namespace WaveBox.Server.Tests {
    public class TranscoderTests {
        // A song with FolderId == null keeps IMediaItemExtensions.FilePath() from hitting the
        // folder repository (it short-circuits to null), so Arguments can be built without
        // Injection.  isDirect: true keeps OutputPath at "-" instead of touching TempFolder.
        private static Song Item(int itemId = 42) {
            return new Song { ItemId = itemId, FolderId = null, FileName = "test.mp3", Duration = 100 };
        }

        private static FFMpegMP3Transcoder Mp3(uint quality, bool isDirect = true) {
            return new FFMpegMP3Transcoder(Item(), quality, isDirect, 0, 0);
        }

        [Theory]
        [InlineData((uint)TranscodeQuality.Low, "-loglevel quiet -i \"\" -c:a libmp3lame -q:a 9 -")]
        [InlineData((uint)TranscodeQuality.Medium, "-loglevel quiet -i \"\" -c:a libmp3lame -q:a 5 -")]
        [InlineData((uint)TranscodeQuality.High, "-loglevel quiet -i \"\" -c:a libmp3lame -q:a 2 -")]
        [InlineData((uint)TranscodeQuality.Extreme, "-loglevel quiet -i \"\" -c:a libmp3lame -q:a 0 -")]
        public void Mp3ArgumentsPerQualityTier(uint quality, string expected) {
            Assert.Equal(expected, Mp3(quality).Arguments);
        }

        [Fact]
        public void Mp3CustomQualityAboveTwelveBecomesConstantBitrate() {
            Assert.Equal("-loglevel quiet -i \"\" -c:a libmp3lame -b:a 327680 -", Mp3(320).Arguments);
        }

        [Theory]
        [InlineData((uint)TranscodeQuality.Low, 64u)]
        [InlineData((uint)TranscodeQuality.Medium, 128u)]
        [InlineData((uint)TranscodeQuality.High, 192u)]
        [InlineData((uint)TranscodeQuality.Extreme, 224u)]
        [InlineData(320u, 320u)]
        public void Mp3EstimatedBitratePerTier(uint quality, uint expected) {
            Assert.Equal(expected, Mp3(quality).EstimatedBitrate);
        }

        [Fact]
        public void AacQualityTiersAllExceedTwelveSoRenderAsBitrates() {
            // Pins actual behavior: the AAC tier values (70/100/120/130) are all > 12, so
            // FFMpegOptionsWith always takes the constant-bitrate branch (quality * 1024)
            FFMpegAACTranscoder low = new FFMpegAACTranscoder(Item(), (uint)TranscodeQuality.Low, true, 0, 0);
            Assert.Equal("-loglevel quiet -i \"\" -c:a aac -b:a 71680 -", low.Arguments);

            FFMpegAACTranscoder extreme = new FFMpegAACTranscoder(Item(), (uint)TranscodeQuality.Extreme, true, 0, 0);
            Assert.Equal("-loglevel quiet -i \"\" -c:a aac -b:a 133120 -", extreme.Arguments);

            Assert.Equal(64u, low.EstimatedBitrate);
            Assert.Equal(224u, extreme.EstimatedBitrate);
        }

        [Theory]
        [InlineData((uint)TranscodeQuality.Low, "-q:a 0", 64u)]
        [InlineData((uint)TranscodeQuality.Medium, "-q:a 5", 128u)]
        [InlineData((uint)TranscodeQuality.High, "-q:a 2", 160u)]
        [InlineData((uint)TranscodeQuality.Extreme, "-q:a 0", 192u)]
        public void OggArgumentsAndBitratePerTier(uint quality, string qualityFlag, uint bitrate) {
            FFMpegOGGTranscoder ogg = new FFMpegOGGTranscoder(Item(), quality, true, 0, 0);

            Assert.Equal("-loglevel quiet -i \"\" -c:a libvorbis " + qualityFlag + " -", ogg.Arguments);
            Assert.Equal(bitrate, ogg.EstimatedBitrate);
        }

        [Theory]
        [InlineData((uint)TranscodeQuality.Low, 64u)]
        [InlineData((uint)TranscodeQuality.Medium, 96u)]
        [InlineData((uint)TranscodeQuality.High, 128u)]
        [InlineData((uint)TranscodeQuality.Extreme, 160u)]
        public void OpusEstimatedBitratePerTier(uint quality, uint expected) {
            // Opus Arguments requires ISongRepository via Injection, so only the
            // Injection-free members are covered here
            FFMpegOpusTranscoder opus = new FFMpegOpusTranscoder(Item(), quality, true, 0, 0);

            Assert.Equal(expected, opus.EstimatedBitrate);
            Assert.Equal("opus", opus.OutputExtension);
            Assert.Equal("audio/opus", opus.MimeType);
        }

        [Fact]
        public void OutputFilenameEncodesItemTypeIdItemIdTypeAndQuality() {
            FFMpegMP3Transcoder transcoder = Mp3((uint)TranscodeQuality.Medium);

            // Song ItemTypeId is 3
            Assert.Equal("3_42_MP3_1.mp3", transcoder.OutputFilename);
        }

        [Fact]
        public void OutputPathIsDashWhenDirectAndNullWithoutItem() {
            Assert.Equal("-", Mp3((uint)TranscodeQuality.Medium).OutputPath);

            FFMpegMP3Transcoder noItem = new FFMpegMP3Transcoder(null, 0, true, 0, 0);
            Assert.Null(noItem.OutputPath);
        }

        [Fact]
        public void MetadataPropertiesPerCodec() {
            FFMpegMP3Transcoder mp3 = Mp3(0);
            Assert.Equal(TranscodeType.MP3, mp3.Type);
            Assert.Equal("audio/mpeg", mp3.MimeType);
            Assert.Equal("ffmpeg", mp3.Command);
            Assert.Equal("libmp3lame", mp3.Codec);

            FFMpegAACTranscoder aac = new FFMpegAACTranscoder(Item(), 0, true, 0, 0);
            Assert.Equal(TranscodeType.AAC, aac.Type);
            Assert.Equal("audio/mp4", aac.MimeType);
            Assert.Equal("mp4", aac.OutputExtension);
        }

        [Fact]
        public void EqualsMatchesSameItemTypeAndQuality() {
            // Two distinct Song instances with the same ItemId compare equal (MediaItem.Equals)
            FFMpegMP3Transcoder a = new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.High, false, 0, 0);
            FFMpegMP3Transcoder b = new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.High, false, 0, 0);

            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void EqualsRejectsDifferentItemQualityOrType() {
            FFMpegMP3Transcoder baseline = new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.High, false, 0, 0);

            Assert.False(baseline.Equals(new FFMpegMP3Transcoder(Item(43), (uint)TranscodeQuality.High, false, 0, 0)));
            Assert.False(baseline.Equals(new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.Low, false, 0, 0)));
            Assert.False(baseline.Equals((object)new FFMpegOGGTranscoder(Item(42), (uint)TranscodeQuality.High, false, 0, 0)));
            Assert.False(baseline.Equals((object)null));
        }

        [Fact]
        public void DirectTranscodersOnlyUseReferenceEquality() {
            FFMpegMP3Transcoder a = new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.High, true, 0, 0);
            FFMpegMP3Transcoder b = new FFMpegMP3Transcoder(Item(42), (uint)TranscodeQuality.High, true, 0, 0);

            Assert.False(a.Equals((object)b));
            Assert.True(a.Equals((object)a));
        }

        [Fact]
        public void GetHashCodeDoesNotThrowWhenItemIsNull() {
            // Regression test for the fixed NRE: Item may legitimately be null
            FFMpegMP3Transcoder transcoder = new FFMpegMP3Transcoder(null, (uint)TranscodeQuality.Medium, true, 0, 0);

            int hash = transcoder.GetHashCode();

            Assert.Equal(hash, transcoder.GetHashCode());
        }

        [Fact]
        public void GetHashCodeDiffersForDifferentItems() {
            FFMpegMP3Transcoder a = new FFMpegMP3Transcoder(Item(1), (uint)TranscodeQuality.High, false, 0, 0);
            FFMpegMP3Transcoder b = new FFMpegMP3Transcoder(Item(2), (uint)TranscodeQuality.High, false, 0, 0);

            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
