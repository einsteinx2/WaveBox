using System;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Core.Tests {
    public class StringExtensionsTests {
        [Theory]
        // Truthy: anything whose trimmed, lowercased form starts with 't' or '1'
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("True", true)]
        [InlineData("t", true)]
        [InlineData("T", true)]
        [InlineData("1", true)]
        [InlineData("  true  ", true)]
        [InlineData("10", true)]
        // Pinned quirk: only the first character is inspected, so any 't'-leading word is true
        [InlineData("tomato", true)]
        [InlineData("this is not a bool", true)]
        // Falsy: everything else
        [InlineData("false", false)]
        [InlineData("f", false)]
        [InlineData("0", false)]
        [InlineData("yes", false)]
        [InlineData("on", false)]
        [InlineData("2", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsTrue_MatchesTruthyMatrix(string input, bool expected) {
            Assert.Equal(expected, input.IsTrue());
        }

        [Fact]
        public void MD5_KnownVector_IsDashSeparatedUppercaseHex() {
            // Pinned: string.MD5() returns BitConverter.ToString output (dash-separated uppercase
            // hex), unlike SHA1() which strips the dashes.  The asymmetry is intentional pinning
            // of current behavior — hashed values are compared against stored values in the same
            // format, so changing it would break existing data.
            Assert.Equal("09-8F-6B-CD-46-21-D3-73-CA-DE-4E-83-26-27-B4-F6", "test".MD5());
        }

        [Fact]
        public void MD5_EmptyOrNull_ReturnsEmptyString() {
            Assert.Equal("", "".MD5());
            Assert.Equal("", ((string)null).MD5());
        }

        [Fact]
        public void SHA1_KnownVector_IsUppercaseHexWithoutDashes() {
            Assert.Equal("A94A8FE5CCB19BA61C4C0873D391E987982FBBD3", "test".SHA1());
        }

        [Fact]
        public void SHA1_EmptyOrNull_ReturnsEmptyString() {
            Assert.Equal("", "".SHA1());
            Assert.Equal("", ((string)null).SHA1());
        }

        [Theory]
        [InlineData("jan", 1)]
        [InlineData("feb", 2)]
        [InlineData("mar", 3)]
        [InlineData("apr", 4)]
        [InlineData("may", 5)]
        [InlineData("jun", 6)]
        [InlineData("jul", 7)]
        [InlineData("aug", 8)]
        [InlineData("sep", 9)]
        [InlineData("oct", 10)]
        [InlineData("nov", 11)]
        [InlineData("dec", 12)]
        // Case-insensitive
        [InlineData("JAN", 1)]
        [InlineData("Dec", 12)]
        // Pinned: only three-letter abbreviations match; full month names return 0
        [InlineData("january", 0)]
        [InlineData("xyz", 0)]
        [InlineData("", 0)]
        public void MonthForAbbreviation_MapsAbbreviationsToMonthNumbers(string input, int expected) {
            Assert.Equal(expected, input.MonthForAbbreviation());
        }

        [Fact]
        public void RemoveByteOrderMark_StripsLeadingUtf8Bom() {
            Assert.Equal("hello", "﻿hello".RemoveByteOrderMark());
        }

        [Fact]
        public void RemoveByteOrderMark_LeavesStringWithoutBomUntouched() {
            // Regression test: the old culture-sensitive StartsWith treated the zero-weight
            // U+FEFF as a prefix of every string, stripping the first real character
            Assert.Equal("hello", "hello".RemoveByteOrderMark());
        }

        [Fact]
        public void RemoveByteOrderMark_EmptyString_ReturnsEmpty() {
            Assert.Equal("", "".RemoveByteOrderMark());
        }

        [Fact]
        public void RemoveByteOrderMark_LeavesMidStringBomUntouched() {
            Assert.Equal("hel﻿lo", "hel﻿lo".RemoveByteOrderMark());
        }
    }
}
