using System;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Core.Tests {
    public class IntExtensionsTests {
        [Theory]
        // Seconds only: always two digits
        [InlineData(0, "00")]
        [InlineData(5, "05")]
        [InlineData(59, "59")]
        // Minutes present: minutes unpadded, seconds two-digit
        [InlineData(60, "1:00")]
        [InlineData(65, "1:05")]
        [InlineData(599, "9:59")]
        [InlineData(600, "10:00")]
        [InlineData(3599, "59:59")]
        // Hours present: minutes always two-digit, even when zero (the fixed behavior)
        [InlineData(3600, "1:00:00")]
        [InlineData(3665, "1:01:05")]
        [InlineData(7200, "2:00:00")]
        [InlineData(86399, "23:59:59")]
        // Pinned: TimeSpan.Hours wraps at 24 and days are dropped, so exactly 24 hours has
        // Hours == 0 and Minutes == 0 and renders as bare seconds "00"
        [InlineData(86400, "00")]
        // Pinned: 25 hours renders as 1 hour (the day component is silently discarded)
        [InlineData(90000, "1:00:00")]
        public void ToTimeString_FormatsDurations(int seconds, string expected) {
            Assert.Equal(expected, seconds.ToTimeString());
        }
    }
}
