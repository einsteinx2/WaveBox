using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class AlbumArtist : IItem, IGroupingItem {
        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return AlbumArtistId; } set { AlbumArtistId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.AlbumArtist; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("albumArtistId")]
        public int? AlbumArtistId { get; set; }

        [JsonPropertyName("albumArtistName")]
        public string AlbumArtistName { get; set; }

        [JsonPropertyName("musicBrainzId")]
        public string MusicBrainzId { get; set; }

        [JsonPropertyName("artId"), IgnoreWrite]
        public int? ArtId { get { return Injection.Get<IArtRepository>().ArtIdForItemId(AlbumArtistId); } }

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
            return Injection.Get<IAlbumRepository>().SearchAlbums("AlbumArtistId", AlbumArtistId.ToString());
        }

        public IList<Song> ListOfSongs() {
            return Injection.Get<ISongRepository>().SearchSongs("AlbumArtistId", AlbumArtistId.ToString());
        }

        public IList<Song> ListOfSingles() {
            return Injection.Get<IAlbumArtistRepository>().SinglesForAlbumArtistId((int)AlbumArtistId);
        }

        public override string ToString() {
            return String.Format("[AlbumArtist: ItemId={0}, AlbumArtistName={1}]", this.ItemId, this.AlbumArtistName);
        }

        public static int CompareAlbumArtistsByName(AlbumArtist x, AlbumArtist y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumArtistName, y.AlbumArtistName);
        }
    }
}
