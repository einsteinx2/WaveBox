using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Linq;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository {
    public class ArtistRepository : IArtistRepository {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDatabase database;
        private readonly IItemRepository itemRepository;

        public ArtistRepository(IDatabase database, IItemRepository itemRepository) {
            if (database == null) {
                throw new ArgumentNullException("database");
            }
            if (itemRepository == null) {
                throw new ArgumentNullException("itemRepository");
            }

            this.database = database;
            this.itemRepository = itemRepository;
        }

        public Artist ArtistForId(int? artistId) {
            return this.database.GetSingle<Artist>("SELECT * FROM Artist WHERE ArtistId = ?", artistId);
        }

        public Artist ArtistForName(string artistName) {
            return this.database.GetSingle<Artist>("SELECT * FROM Artist WHERE ArtistName = ?", artistName);
        }

        public bool InsertArtist(string artistName, bool replace = false) {
            int? itemId = itemRepository.GenerateItemId(ItemType.Artist);
            if (itemId == null) {
                return false;
            }

            Artist artist = new Artist();
            artist.ArtistId = itemId;
            artist.ArtistName = artistName;
            int affected = this.database.InsertObject<Artist>(artist, replace ? InsertType.Replace : InsertType.InsertOrIgnore);

            return affected > 0;
        }

        public bool InsertArtist(Artist artist, bool replace = false) {
            return this.database.InsertObject<Artist>(artist, replace ? InsertType.Replace : InsertType.InsertOrIgnore) > 0;
        }

        public Artist ArtistForNameOrCreate(string artistName) {
            if (artistName == null || artistName == "") {
                return new Artist();
            }

            // check to see if the artist exists
            Artist anArtist = this.ArtistForName(artistName);

            // if not, create it.
            if (anArtist.ArtistId == null) {
                anArtist = null;
                if (this.InsertArtist(artistName)) {
                    anArtist = this.ArtistForNameOrCreate(artistName);
                } else {
                    // The insert failed because this album was inserted by another
                    // thread, so grab the artist id, it will exist this time
                    anArtist = this.ArtistForName(artistName);
                }
            }

            // then return the artist object retrieved or created.
            return anArtist;
        }

        public IList<Artist> AllArtists() {
            return this.database.GetList<Artist>("SELECT * FROM Artist ORDER BY ArtistName COLLATE NOCASE");
        }

        public int CountArtists() {
            return this.database.GetScalar<int>("SELECT COUNT(ArtistId) FROM Artist");
        }

        public IList<Artist> SearchArtists(string field, string query, bool exact = true) {
            if (query == null) {
                return new List<Artist>();
            }

            // Set default field, if none provided
            if (field == null) {
                field = "ArtistName";
            }

            // Check to ensure a valid query field was set
            if (!new string[] {"ArtistId", "ArtistName"} .Contains(field)) {
                return new List<Artist>();
            }

            if (exact) {
                // Search for exact match
                return this.database.GetList<Artist>("SELECT * FROM Artist WHERE " + field + " = ? ORDER BY ArtistName COLLATE NOCASE", query);
            }

            // Search for fuzzy match (containing query)
            return this.database.GetList<Artist>("SELECT * FROM Artist WHERE " + field + " LIKE ? ORDER BY ArtistName COLLATE NOCASE", "%" + query + "%");
        }

        // Return a list of artists titled between a range of (a-z, A-Z, 0-9 characters)
        public IList<Artist> RangeArtists(char start, char end) {
            // Ensure characters are alphanumeric, return empty list if either is not
            if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end)) {
                return new List<Artist>();
            }

            string s = start.ToString();
            // Add 1 to character to make end inclusive
            string en = Convert.ToChar((int)end + 1).ToString();

            return this.database.GetList<Artist>(
                       "SELECT * FROM Artist " +
                       "WHERE Artist.ArtistName BETWEEN LOWER(?) AND LOWER(?) " +
                       "OR Artist.ArtistName BETWEEN UPPER(?) AND UPPER(?)" +
                       "ORDER BY ArtistName COLLATE NOCASE",
                       s, en, s, en);
        }

        // Return a list of artists using SQL LIMIT x,y where X is starting index and Y is duration
        public IList<Artist> LimitArtists(int index, int duration = Int32.MinValue) {
            string query = "SELECT * FROM Artist ORDER BY ArtistName COLLATE NOCASE LIMIT ? ";

            // Add duration to LIMIT if needed
            if (duration != Int32.MinValue && duration > 0) {
                query += ", ?";
            }

            return this.database.GetList<Artist>(query, index, duration);
        }

        public IList<Album> AlbumsForArtistId(int artistId) {
            return this.database.GetList<Album>(
                       "SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Song " +
                       "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
                       "LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
                       "LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
                       "LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
                       "WHERE Song.ArtistId = ? GROUP BY Album.AlbumId ORDER BY Album.AlbumName COLLATE NOCASE",
                       artistId);
        }

        public IList<Artist> AllWithNoMusicBrainzId() {
            return this.database.GetList<Artist>("SELECT * FROM Artist WHERE MusicBrainzId IS NULL");
        }
    }
}
