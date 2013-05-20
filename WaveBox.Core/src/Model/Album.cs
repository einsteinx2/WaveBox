using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.Static;
using Newtonsoft.Json;

namespace WaveBox.Model
{
	public class Album : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore]
		public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

		[JsonIgnore]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId")]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get { return DetermineArtId(); } }

		public Album()
		{
		}

		public Album(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		public Album(int albumId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE album_id = @albumid", conn);
				q.AddNamedParam("@albumid", albumId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Album(string albumName, int? artistId)
		{
			if (albumName == null || albumName == "")
			{
				return;
			}

			AlbumName = albumName;
			ArtistId = artistId;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE album_name = @albumname AND artist_id = @artistid", conn);
				q.AddNamedParam("@albumname", AlbumName);
				q.AddNamedParam("@artistid", ArtistId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public int? DetermineArtId()
		{
			// Return the art id (if any) for this album, based on the current best art id using either a song art id or the folder art id

			List<int> songArtIds = SongArtIds();
			if (songArtIds.Count == 1)
			{
				// There is one unique art ID for these songs, so return it
				return songArtIds[0];
			}
			else
			{
				// Check the folder art id(s)
				List<int> folderArtIds = FolderArtIds();
				if (folderArtIds.Count == 0)
				{
					// There is no folder art
					if (songArtIds.Count == 0)
					{
						// There is no art for this album
						return null;
					}
					else
					{
						// For now, just return the first art id
						return songArtIds[0];
					}
				}
				else if (folderArtIds.Count == 1)
				{
					// There are multiple different art ids for the songs, but only one folder art, so use that. Likely this is a compilation album
					return folderArtIds[0];
				}
				else 
				{
					// There are multiple song and folder art ids, so let's just return the first song art id for now
					return songArtIds[0];
				}
			}
		}

		private List<int> SongArtIds()
		{
			List<int> artIds = new List<int>();
			
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_item.art_id FROM song " +
													 "LEFT JOIN art_item ON song_id = art_item.item_id " +
													 "WHERE song.song_album_id = @albumid AND art_item.art_id IS NOT NULL GROUP BY art_item.art_id", conn);
				q.AddNamedParam("@albumid", AlbumId);
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					artIds.Add(reader.GetInt32(0));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return artIds;
		}

		private List<int> FolderArtIds()
		{
			List<int> artIds = new List<int>();
			
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_item.art_id FROM song " +
													 "LEFT JOIN art_item ON song_folder_id = art_item.item_id " +
													 "WHERE song.song_album_id = @albumid AND art_item.art_id IS NOT NULL GROUP BY art_item.art_id", conn);
				q.AddNamedParam("@albumid", AlbumId);
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					artIds.Add(reader.GetInt32(0));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return artIds;
		}

		private static bool InsertAlbum(string albumName, int? artistId, int? releaseYear)
		{
			int? itemId = Item.GenerateItemId(ItemType.Album);
			if (itemId == null)
			{
				return false;
			}

			bool success = false;

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO album (album_id, album_name, artist_id, album_release_year) VALUES (@albumid, @albumname, @artistid, @albumreleaseyear)", conn);
				q.AddNamedParam("@albumid", itemId);
				q.AddNamedParam("@albumname", albumName);
				q.AddNamedParam("@artistid", artistId);
				q.AddNamedParam("@albumreleaseyear", releaseYear);
				q.Prepare();

				success = (q.ExecuteNonQueryLogged() > 0);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return success;
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			ArtistId = reader.GetInt32OrNull(reader.GetOrdinal("artist_id"));
			AlbumId = reader.GetInt32OrNull(reader.GetOrdinal("album_id"));
			AlbumName = reader.GetStringOrNull(reader.GetOrdinal("album_name"));
			ReleaseYear = reader.GetInt32OrNull(reader.GetOrdinal("album_release_year"));
		}

		public Artist Artist()
		{
			return new Artist(ArtistId);
		}

		// TO DO
		public void AutoTag()
		{
		}

		public List<Song> ListOfSongs()
		{
			List<Song> songs = new List<Song>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name, genre.genre_name FROM song " +
													 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
													 "LEFT JOIN album ON song_album_id = album.album_id " +
													 "LEFT JOIN genre ON song_genre_id = genre.genre_id " +
													 "WHERE song_album_id = @albumid", conn);
				q.AddNamedParam("@albumid", AlbumId);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songs.Add(new Song(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public static Album AlbumForName(string albumName, int? artistId, int? releaseYear = null)
		{
			if (albumName == "" || albumName == null || artistId == null)
			{
				return new Album();
			}

			Album a = new Album(albumName, artistId);

			if (a.AlbumId == null)
			{
				a = null;
				if (InsertAlbum(albumName, artistId, releaseYear))
				{
					a = AlbumForName(albumName, artistId, releaseYear);
				}
				else
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the album id, it will exist this time
					a = new Album(albumName, artistId);
				}
			}

			return a;
		}

		public static List<Album> AllAlbums()
		{
			List<Album> albums = new List<Album>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					albums.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			albums.Sort(CompareAlbumsByName);
			return albums;
		}

		public static int? CountAlbums()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			int? count = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(album_id) FROM album", conn);
				count = Convert.ToInt32(q.ExecuteScalar());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return count;
		}
		
		public static List<Album> SearchAlbums(string field, string query, bool exact = true)
		{
			List<Album> results = new List<Album>();

			if (query == null)
			{
				return results;
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "album_name";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"album_id", "album_name", "artist_id", "album_release_year"}.Contains(field))
			{
				return results;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = null;

				// Search for exact match
				if (exact)
				{
					q = Database.GetDbCommand("SELECT * FROM album WHERE " + field + " = @query", conn);
					q.AddNamedParam("@query", query);
				}
				// Search for fuzzy match (containing query)
				else
				{
					q = Database.GetDbCommand("SELECT * FROM album WHERE " + field + " LIKE @query", conn);
					q.AddNamedParam("@query", "%" + query + "%");
				}

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					results.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return results;
		}

		public static List<Album> RandomAlbums()
		{
			List<Album> random = new List<Album>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(*) FROM album ORDER BY NEWID() LIMIT 1", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					random.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return random;
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}
	}
}
