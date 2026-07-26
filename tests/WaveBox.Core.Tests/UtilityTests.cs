using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using Xunit;

namespace WaveBox.Core.Tests {
    public class UtilityTests {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()";

        private class GroupingStub : IGroupingItem {
            public string GroupingName { get; set; }
        }

        [Fact]
        public void RandomString_HasRequestedLengthAndAlphabet() {
            string s = Utility.RandomString(20);
            Assert.Equal(20, s.Length);
            Assert.All(s, c => Assert.Contains(c, Alphabet));
        }

        [Fact]
        public void RandomString_ZeroLength_ReturnsEmptyString() {
            Assert.Equal("", Utility.RandomString(0));
        }

        [Fact]
        public void RandomString_IsThreadSafeUnderParallelGeneration() {
            // The fixed implementation uses Random.Shared, which is thread-safe.  The old shared
            // Random instance corrupted under concurrent use (returning wrong-length/degenerate
            // output) and these strings feed session IDs.  320 random strings of length 20 over
            // a 45-char alphabet make an accidental duplicate essentially impossible (~1e-30).
            const int count = 320;
            string[] results = new string[count];

            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 32 }, i => {
                results[i] = Utility.RandomString(20);
            });

            Assert.All(results, s => {
                Assert.NotNull(s);
                Assert.Equal(20, s.Length);
                Assert.All(s, c => Assert.Contains(c, Alphabet));
            });
            Assert.Equal(count, results.Distinct().Count());
        }

        [Fact]
        public void SectionPositionsFromSortedList_NullList_ReturnsEmptyPairList() {
            PairList<string, int> positions = Utility.SectionPositionsFromSortedList(null);
            Assert.NotNull(positions);
            Assert.Empty(positions);
        }

        [Fact]
        public void SectionPositionsFromSortedList_EmptyList_ReturnsEmptyPairList() {
            Assert.Empty(Utility.SectionPositionsFromSortedList(new List<IGroupingItem>()));
        }

        [Fact]
        public void SectionPositionsFromSortedList_RecordsFirstIndexPerInitialLetter() {
            IList<IGroupingItem> sorted = new List<IGroupingItem> {
                new GroupingStub { GroupingName = "Apple" },    // 0: first A
                new GroupingStub { GroupingName = "Avocado" },  // 1
                new GroupingStub { GroupingName = "Banana" },   // 2: first B
                new GroupingStub { GroupingName = "cherry" },   // 3: first C (case-insensitive)
                new GroupingStub { GroupingName = "Cranberry" } // 4
            };

            PairList<string, int> positions = Utility.SectionPositionsFromSortedList(sorted);

            Assert.Equal(3, positions.Count);
            Assert.Equal(new KeyValuePair<string, int>("A", 0), positions[0]);
            Assert.Equal(new KeyValuePair<string, int>("B", 2), positions[1]);
            // Keys are uppercased even when the grouping name is lowercase
            Assert.Equal(new KeyValuePair<string, int>("C", 3), positions[2]);
        }

        [Fact]
        public void SectionPositionsFromSortedList_SkipsNullAndEmptyGroupingNames() {
            IList<IGroupingItem> sorted = new List<IGroupingItem> {
                new GroupingStub { GroupingName = null },
                new GroupingStub { GroupingName = "" },
                new GroupingStub { GroupingName = "Zebra" } // index 2 despite skipped entries
            };

            PairList<string, int> positions = Utility.SectionPositionsFromSortedList(sorted);

            Assert.Single(positions);
            Assert.Equal(new KeyValuePair<string, int>("Z", 2), positions[0]);
        }
    }
}
