using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class Album : IItem, IGroupingItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Album; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("albumArtistId")]
        public int? AlbumArtistId { get; set; }

        [JsonPropertyName("albumArtistName"), IgnoreWrite]
        public string AlbumArtistName { get; set; }

        [JsonPropertyName("albumId")]
        public int? AlbumId { get; set; }

        [JsonPropertyName("albumName")]
        public string AlbumName { get; set; }

        [JsonPropertyName("releaseYear")]
        public int? ReleaseYear { get; set; }

        [JsonPropertyName("musicBrainzId")]
        public string MusicBrainzId { get; set; }

        [JsonPropertyName("artId"), IgnoreWrite]
        public int? ArtId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public string GroupingName { get { return AlbumName; } }

        public Album() {
        }

        public AlbumArtist AlbumArtist() {
            return Injection.Get<IAlbumArtistRepository>().AlbumArtistForId(AlbumArtistId);
        }

        public IList<Song> ListOfSongs() {
            return Injection.Get<ISongRepository>().SearchSongs("AlbumId", AlbumId.ToString());
        }

        public override string ToString() {
            return String.Format("[Album: ItemId={0}, AlbumName={1}]", this.ItemId, this.AlbumName);
        }

        public static int CompareAlbumsByName(Album x, Album y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
        }
    }
}
