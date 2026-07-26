using System;
using System.IO;
using System.Text;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Server.Tests {
    public class StreamExtensionsTests {
        [Fact]
        public void Md5OfEmptyStreamIsKnownVector() {
            using (MemoryStream stream = new MemoryStream()) {
                Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", stream.MD5());
            }
        }

        [Fact]
        public void Md5OfKnownInputIsLowercaseHex() {
            using (MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes("abc"))) {
                Assert.Equal("900150983cd24fb0d6963f7d28e17f72", stream.MD5());
            }
        }
    }
}
