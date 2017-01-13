using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class AlbumArtist : IItem, IGroupingItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return AlbumArtistId; } set { AlbumArtistId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.AlbumArtist; } }

        [JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonProperty("albumArtistId")]
        public int? AlbumArtistId { get; set; }

        [JsonProperty("albumArtistName")]
        public string AlbumArtistName { get; set; }

        [JsonProperty("musicBrainzId")]
        public string MusicBrainzId { get; set; }

        [JsonProperty("artId"), IgnoreWrite]
        public int? ArtId { get { return Injection.Kernel.Get<IArtRepository>().ArtIdForItemId(AlbumArtistId); } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public string GroupingName { get { return AlbumArtistName; } }

        /// <summary>
        /// Constructors
        /// </summary>

        public AlbumArtist() {
        }

        /// <summary>
        /// Public methods
        /// </summary>

        public IList<Album> ListOfAlbums() {
            return Injection.Kernel.Get<IAlbumRepository>().SearchAlbums("AlbumArtistId", AlbumArtistId.ToString());
        }

        public IList<Song> ListOfSongs() {
            return Injection.Kernel.Get<ISongRepository>().SearchSongs("AlbumArtistId", AlbumArtistId.ToString());
        }

        public IList<Song> ListOfSingles() {
            return Injection.Kernel.Get<IAlbumArtistRepository>().SinglesForAlbumArtistId((int)AlbumArtistId);
        }

        public override string ToString() {
            return String.Format("[AlbumArtist: ItemId={0}, AlbumArtistName={1}]", this.ItemId, this.AlbumArtistName);
        }

        public static int CompareAlbumArtistsByName(AlbumArtist x, AlbumArtist y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumArtistName, y.AlbumArtistName);
        }
    }
}
