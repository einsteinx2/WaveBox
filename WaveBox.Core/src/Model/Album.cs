using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public class Album : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore]
		public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		[JsonProperty("artId"), IgnoreWrite]
		public int? ArtId { get { return DetermineArtId(); } }

		public Album()
		{
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
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<int>("SELECT ArtItem.art_id FROM song " +
				                       "LEFT JOIN ArtItem ON song.ItemId = ArtItem.item_id " +
				                       "WHERE song.AlbumId = ? AND ArtItem.art_id IS NOT NULL GROUP BY ArtItem.art_id", AlbumId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}
			
			return new List<int>();
		}

		private List<int> FolderArtIds()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<int>("SELECT ArtItem.art_id FROM song " +
									   "LEFT JOIN ArtItem ON song.FolderId = ArtItem.item_id " +
				                       "WHERE song.AlbumId = ? AND ArtItem.art_id IS NOT NULL GROUP BY ArtItem.art_id", AlbumId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}
			
			return new List<int>();
		}

		private static bool InsertAlbum(string albumName, int? artistId, int? releaseYear)
		{
			int? itemId = Item.GenerateItemId(ItemType.Album);
			if (itemId == null)
			{
				return false;
			}

			bool success = false;

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				Album album = new Album();
				album.AlbumName = albumName;
				album.ArtistId = artistId;
				album.ReleaseYear = releaseYear;
				success = conn.InsertLogged(album, InsertType.InsertOrIgnore) > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
				success = false;
			}
			finally
			{
				conn.Close();
			}

			return success;
		}

		public Artist Artist()
		{
			return new Artist.Factory().CreateArtist(ArtistId);
		}

		// TO DO
		public void AutoTag()
		{
		}

		public List<Song> ListOfSongs()
		{
			return Song.SearchSongs("AlbumId", AlbumId.ToString());
		}

		public static Album AlbumForName(string albumName, int? artistId, int? releaseYear = null)
		{
			if (albumName == "" || albumName == null || artistId == null)
			{
				return new Album();
			}

			Album a = new Album.Factory().CreateAlbum(albumName, artistId);

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
					a = new Album.Factory().CreateAlbum(albumName, artistId);
				}
			}

			return a;
		}

		public static List<Album> AllAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Album>("SELECT * FROM album ORDER BY AlbumName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Album>();
		}

		public static int CountAlbums()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT count(ItemId) FROM album");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}
		
		public static List<Album> SearchAlbums(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				return new List<Album>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "AlbumName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"ItemId", "AlbumName", "ArtistId", "ReleaseYear"}.Contains(field))
			{
				return new List<Album>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();

				if (exact)
				{
					// Search for exact match
					return conn.Query<Album>("SELECT * FROM album WHERE " + field + " = ? ORDER BY AlbumName", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					return conn.Query<Album>("SELECT * FROM album WHERE " + field + " LIKE ? ORDER BY AlbumName", "%" + query + "%");
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Album>();
		}

		public static List<Album> RandomAlbums(int limit = 10)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Album>("SELECT * FROM album ORDER BY RANDOM() LIMIT " + limit, conn);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Album>();
		}

		public static int CompareAlbumsByName(Album x, Album y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.AlbumName, y.AlbumName);
		}

		public class Factory
		{
			public Factory()
			{
			}

			public Album CreateAlbum(int albumId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT * FROM album WHERE ItemId = ?", albumId);

					foreach (Album a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return new Album();
			}

			public Album CreateAlbum(string albumName, int? artistId)
			{
				if (albumName == null || albumName == "")
				{
					return new Album();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT * FROM album WHERE AlbumName = ? AND ArtistId = ?", albumName, artistId);

					foreach (Album a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				Album album = new Album();
				album.AlbumName = albumName;
				album.ArtistId = artistId;
				return album;
			}
		}
	}
}
