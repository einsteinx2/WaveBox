using System;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using Xunit;

namespace WaveBox.Core.Tests {
    public class FileTypeExtensionsTests {
        [Theory]
        [InlineData("taglib/m4a", FileType.AAC)]
        [InlineData("taglib/aac", FileType.AAC)]
        [InlineData("taglib/mp3", FileType.MP3)]
        [InlineData("taglib/mpc", FileType.MPC)]
        // Pinned: the OGG mapping is the literal "taglib/oggo" (matches TagLib#'s registered
        // extension-derived mime for .oggo); a plain "taglib/ogg" is NOT mapped — see below
        [InlineData("taglib/oggo", FileType.OGG)]
        [InlineData("taglib/wma", FileType.WMA)]
        [InlineData("taglib/flac", FileType.FLAC)]
        [InlineData("taglib/wv", FileType.WV)]
        [InlineData("taglib/ape", FileType.APE)]
        // Video types match by substring, so codec suffixes still map
        [InlineData("taglib/mp4", FileType.MP4)]
        [InlineData("taglib/mp4; codecs=avc1", FileType.MP4)]
        [InlineData("taglib/mkv", FileType.MKV)]
        [InlineData("taglib/avi", FileType.AVI)]
        // Pinned quirk: "taglib/ogg" (without the trailing 'o') is unknown
        [InlineData("taglib/ogg", FileType.Unknown)]
        [InlineData("audio/mpeg", FileType.Unknown)]
        [InlineData("", FileType.Unknown)]
        public void FileTypeForTagLibMimeType_MapsMimeTypes(string mimeType, FileType expected) {
            Assert.Equal(expected, default(FileType).FileTypeForTagLibMimeType(mimeType));
        }

        [Theory]
        [InlineData(FileType.AAC, "audio/mp4")]
        [InlineData(FileType.MP3, "audio/mpeg")]
        [InlineData(FileType.MPC, "audio/mpc")]
        [InlineData(FileType.OGG, "audio/ogg")]
        [InlineData(FileType.WMA, "audio/wma")]
        [InlineData(FileType.ALAC, "audio/alac")]
        [InlineData(FileType.FLAC, "audio/flac")]
        [InlineData(FileType.WV, "audio/wv")]
        [InlineData(FileType.APE, "audio/ape")]
        [InlineData(FileType.MP4, "video/mp4")]
        [InlineData(FileType.MKV, "video/mkv")]
        [InlineData(FileType.AVI, "video/avi")]
        [InlineData(FileType.Unknown, "text/plain")]
        public void MimeType_MapsFileTypes(FileType fileType, string expected) {
            Assert.Equal(expected, fileType.MimeType());
        }

        [Theory]
        [InlineData(1, FileType.AAC)]
        [InlineData(2, FileType.MP3)]
        [InlineData(3, FileType.MPC)]
        [InlineData(4, FileType.OGG)]
        [InlineData(5, FileType.WMA)]
        [InlineData(6, FileType.ALAC)]
        [InlineData(7, FileType.APE)]
        [InlineData(8, FileType.FLAC)]
        [InlineData(9, FileType.WV)]
        [InlineData(10, FileType.MP4)]
        [InlineData(11, FileType.MKV)]
        [InlineData(12, FileType.AVI)]
        [InlineData(2147483647, FileType.Unknown)]
        // Unknown ids fall through to Unknown
        [InlineData(0, FileType.Unknown)]
        [InlineData(13, FileType.Unknown)]
        [InlineData(-1, FileType.Unknown)]
        public void FileTypeForId_MapsIds(int id, FileType expected) {
            Assert.Equal(expected, default(FileType).FileTypeForId(id));
        }
    }
}
