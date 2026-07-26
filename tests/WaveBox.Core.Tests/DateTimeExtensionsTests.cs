using System;
using System.Text.RegularExpressions;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Core.Tests {
    // Pinning note: the implementations funnel through ToLocalTime()/ToUniversalTime() on
    // DateTimes with Kind == Unspecified, so absolute round-trips (long -> DateTime -> long)
    // are shifted by the machine's UTC offset in non-UTC timezones.  We deliberately pin only
    // timezone-neutral properties here (kinds, deltas, and values where the offset cancels)
    // rather than "fixing" the behavior, since stored timestamps depend on it.
    public class DateTimeExtensionsTests {
        [Fact]
        public void ToDateTime_ReturnsUtcKind() {
            Assert.Equal(DateTimeKind.Utc, 0L.ToDateTime().Kind);
            Assert.Equal(DateTimeKind.Utc, 1000000000L.ToDateTime().Kind);
        }

        [Fact]
        public void ToUnixTime_OfUnspecifiedEpoch_IsZero() {
            // Timezone-neutral: both sides of the subtraction interpret the same Unspecified
            // DateTime(1970,1,1) identically, so the local offset cancels exactly
            Assert.Equal(0L, new DateTime(1970, 1, 1).ToUnixTime());
        }

        [Fact]
        public void ToUnixTime_OfUnspecifiedEpochPlusOneDay_Is86400() {
            // Neutral in practice: the local UTC offset on 1970-01-01 and 1970-01-02 is the same
            Assert.Equal(86400L, new DateTime(1970, 1, 2).ToUnixTime());
        }

        [Fact]
        public void ToUnixTime_PreservesDifferences() {
            DateTime baseTime = new DateTime(2001, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(90L, baseTime.AddSeconds(90).ToUnixTime() - baseTime.ToUnixTime());
            Assert.Equal(3600L, baseTime.AddHours(1).ToUnixTime() - baseTime.ToUnixTime());
        }

        [Fact]
        public void ToDateTime_PreservesDifferences() {
            // Both instants fall on the same local day, so the offset applied is identical
            Assert.Equal(TimeSpan.FromHours(1), 3600L.ToDateTime() - 0L.ToDateTime());
            Assert.Equal(TimeSpan.FromSeconds(90), 1090L.ToDateTime() - 1000L.ToDateTime());
        }

        [Fact]
        public void RoundTrip_PreservesDifferences() {
            // The absolute round-trip is offset-shifted in non-UTC zones (see class comment),
            // but the shift is constant, so differences survive the round-trip
            long a = 1000L.ToDateTime().ToUnixTime();
            long b = 0L.ToDateTime().ToUnixTime();
            Assert.Equal(1000L, a - b);
        }

        [Fact]
        public void ToRFC1123_FormatsUtcDateTime() {
            DateTime dt = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            Assert.Equal("Sat, 03 Feb 2001 04:05:06 GMT", dt.ToRFC1123());
        }

        [Fact]
        public void ToETag_IsSha1OfRFC1123String() {
            DateTime dt = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            string etag = dt.ToETag();
            Assert.Equal(dt.ToRFC1123().SHA1(), etag);
            // 40 lowercase hex chars
            Assert.Matches(new Regex("^[0-9a-f]{40}$"), etag);
        }
    }
}
