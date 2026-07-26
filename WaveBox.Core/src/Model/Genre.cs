using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;
using System.Text.Json.Serialization;

namespace WaveBox.Core.Model {
    public class Genre : IItem, IGroupingItem {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return GenreId; } set { GenreId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Genre; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("genreId")]
        public int? GenreId { get; set; }

        [JsonPropertyName("genreName")]
        public string GenreName { get; set; }

        // Currently unused, only to satisfy IItem interface requirements
        [JsonPropertyName("artId"), IgnoreRead, IgnoreWrite]
        public int? ArtId { get; set; }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public string GroupingName { get { return GenreName; } }

        public IList<Artist> ListOfArtists() {
            if (GenreId == null) {
                return new List<Artist>();
            }

            return Injection.Get<IGenreRepository>().ListOfArtists((int)GenreId);
        }

        public IList<Album> ListOfAlbums() {
            if (GenreId == null) {
                return new List<Album>();
            }

            return Injection.Get<IGenreRepository>().ListOfAlbums((int)GenreId);
        }

        public IList<Song> ListOfSongs() {
            if (GenreId == null) {
                return new List<Song>();
            }

            return Injection.Get<IGenreRepository>().ListOfSongs((int)GenreId);
        }

        public IList<Folder> ListOfFolders() {
            if (GenreId == null) {
                return new List<Folder>();
            }

            return Injection.Get<IGenreRepository>().ListOfFolders((int)GenreId);
        }

        public override string ToString() {
            return String.Format("[Genre: GenreId={0}, GenreName={1}]", this.GenreId, this.GenreName);
        }

        public static int CompareGenresByName(Genre x, Genre y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.GenreName, y.GenreName);
        }
    }
}
