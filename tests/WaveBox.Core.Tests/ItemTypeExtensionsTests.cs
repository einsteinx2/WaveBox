using System;
using WaveBox.Core.Model;
using Xunit;

namespace WaveBox.Core.Tests {
    public class ItemTypeExtensionsTests {
        [Theory]
        [InlineData(1, ItemType.Artist)]
        [InlineData(2, ItemType.Album)]
        [InlineData(3, ItemType.Song)]
        [InlineData(4, ItemType.Folder)]
        [InlineData(5, ItemType.Playlist)]
        [InlineData(6, ItemType.PlaylistItem)]
        [InlineData(7, ItemType.Podcast)]
        [InlineData(8, ItemType.PodcastEpisode)]
        [InlineData(9, ItemType.User)]
        [InlineData(10, ItemType.Video)]
        [InlineData(11, ItemType.Bookmark)]
        [InlineData(12, ItemType.BookmarkItem)]
        [InlineData(13, ItemType.Art)]
        [InlineData(14, ItemType.Genre)]
        [InlineData(15, ItemType.AlbumArtist)]
        [InlineData(16, ItemType.Favorite)]
        [InlineData(2147483647, ItemType.Unknown)]
        // Unknown ids fall through to Unknown
        [InlineData(0, ItemType.Unknown)]
        [InlineData(17, ItemType.Unknown)]
        [InlineData(-5, ItemType.Unknown)]
        public void ItemTypeForId_MapsIds(int id, ItemType expected) {
            Assert.Equal(expected, ItemTypeExtensions.ItemTypeForId(id));
        }
    }
}
