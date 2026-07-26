using System;
using System.Text;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Core.Tests {
    public class ByteExtensionsTests {
        [Fact]
        public void MD5_KnownVector_IsLowercaseHex() {
            byte[] input = Encoding.ASCII.GetBytes("test");
            Assert.Equal("098f6bcd4621d373cade4e832627b4f6", input.MD5());
        }

        [Fact]
        public void MD5_Abc_MatchesKnownVector() {
            byte[] input = Encoding.ASCII.GetBytes("abc");
            Assert.Equal("900150983cd24fb0d6963f7d28e17f72", input.MD5());
        }

        [Fact]
        public void MD5_EmptyArray_MatchesKnownVector() {
            Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", Array.Empty<byte>().MD5());
        }
    }
}
