using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core.Extensions;
using Xunit;

namespace WaveBox.Core.Tests {
    public class IListExtensionsTests {
        [Fact]
        public void AddRange_AppendsItemsInOrder() {
            IList<int> list = new List<int> { 1, 2 };
            list.AddRange(new List<int> { 3, 4, 5 });
            Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, list);
        }

        [Fact]
        public void AddRange_EmptyInput_LeavesListUnchanged() {
            IList<int> list = new List<int> { 1, 2 };
            list.AddRange(Array.Empty<int>());
            Assert.Equal(new List<int> { 1, 2 }, list);
        }

        [Fact]
        public void InsertRange_InsertsItemsInOrderAtIndex() {
            IList<int> list = new List<int> { 1, 5 };
            list.InsertRange(1, new List<int> { 2, 3, 4 });
            Assert.Equal(new List<int> { 1, 2, 3, 4, 5 }, list);
        }

        [Fact]
        public void InsertRange_AtZero_PrependsItems() {
            IList<int> list = new List<int> { 3, 4 };
            list.InsertRange(0, new List<int> { 1, 2 });
            Assert.Equal(new List<int> { 1, 2, 3, 4 }, list);
        }

        [Fact]
        public void Shuffle_PreservesMultiset() {
            // Includes duplicates so we verify the multiset, not just the set
            List<int> original = new List<int>();
            for (int i = 0; i < 50; i++) {
                original.Add(i % 10);
            }

            List<int> shuffled = new List<int>(original);
            shuffled.Shuffle();

            Assert.Equal(original.Count, shuffled.Count);
            Assert.Equal(original.OrderBy(x => x), shuffled.OrderBy(x => x));
        }

        [Fact]
        public void Shuffle_EmptyAndSingleElementLists_AreNoOps() {
            List<int> empty = new List<int>();
            empty.Shuffle();
            Assert.Empty(empty);

            List<int> single = new List<int> { 42 };
            single.Shuffle();
            Assert.Single(single);
            Assert.Equal(42, single[0]);
        }

        [Fact]
        public void ToCSV_JoinsWithCommaSpace() {
            IList<string> list = new List<string> { "a", "b", "c" };
            Assert.Equal("a, b, c", list.ToCSV());
        }

        [Fact]
        public void ToCSV_Quoted_WrapsEachItemInDoubleQuotes() {
            IList<string> list = new List<string> { "a", "b", "c" };
            Assert.Equal("\"a\", \"b\", \"c\"", list.ToCSV(true));
        }

        [Fact]
        public void ToCSV_SingleItem_HasNoTrailingSeparator() {
            IList<string> list = new List<string> { "a" };
            Assert.Equal("a", list.ToCSV());
            Assert.Equal("\"a\"", list.ToCSV(true));
        }

        [Fact]
        public void ToCSV_EmptyList_ReturnsEmptyOrEmptyQuotes() {
            IList<string> list = new List<string>();
            Assert.Equal("", list.ToCSV());
            Assert.Equal("\"\"", list.ToCSV(true));
        }
    }
}
