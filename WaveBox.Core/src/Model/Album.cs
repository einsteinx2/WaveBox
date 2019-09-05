using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using Newtonsoft.Json;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class Album : IItem, IGroupingItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Album; } }

        [JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonProperty("albumArtistId")]
        public int? AlbumArtistId { get; set; }

        [JsonProperty("albumArtistName"), IgnoreWrite]
        public string AlbumArtistName { get; set; }

        [JsonProperty("albumId")]
        public int? AlbumId { get; set; }

        [JsonProperty("albumName")]
        public string AlbumName { get; set; }

        [JsonProperty("releaseYear")]
        public int? ReleaseYear { get; set; }

        [JsonProperty("musicBrainzId")]
        public string MusicBrainzId { get; set; }

        [JsonProperty("artId"), IgnoreWrite]
        public int? ArtId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public string GroupingName { get { return AlbumName; } }

        public Album() {
        }

        public AlbumArtist AlbumArtist() {
            return Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForId(AlbumArtistId);
        }

        public IList<Song> ListOfSongs() {
            return Injection.Kernel.Get<ISongRepository>().SearchSongs("AlbumId", AlbumId.ToString());
        }

        public override string ToString() {
            return String.Format("[Album: ItemId={0}, AlbumName={1}]", this.ItemId, this.AlbumName);
        }

        public static int CompareAlbumsByName(Album x, Album y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
        }
    }
}
