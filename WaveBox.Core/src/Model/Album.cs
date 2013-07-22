using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using Newtonsoft.Json;
using WaveBox.Core.Injection;
using WaveBox.Static;
using WaveBox.Model.Repository;

namespace WaveBox.Model
{
	public class Album : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return AlbumId; } set { AlbumId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Album; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName"), IgnoreWrite]
		public string ArtistName { get; set; }

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
			if (AlbumId == null)
				return new List<int>();

			return Injection.Kernel.Get<IAlbumRepository>().SongArtIds((int)AlbumId);
		}

		private List<int> FolderArtIds()
		{
			if (AlbumId == null)
				return new List<int>();

			return Injection.Kernel.Get<IAlbumRepository>().FolderArtIds((int)AlbumId);
		}

		public Artist Artist()
		{
			return new Artist.Factory().CreateArtist(ArtistId);
		}

		public List<Song> ListOfSongs()
		{
			return Injection.Kernel.Get<ISongRepository>().SearchSongs("AlbumId", AlbumId.ToString());
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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
															"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
															"WHERE Album.AlbumId = ?", albumId);

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
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Album>("SELECT Album.*, Artist.ArtistName FROM Album " +
															"LEFT JOIN Artist ON Album.ArtistId = Artist.ArtistId " +
															"WHERE Album.AlbumName = ? AND Album.ArtistId = ?", albumName, artistId);

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
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				Album album = new Album();
				album.AlbumName = albumName;
				album.ArtistId = artistId;
				return album;
			}
		}
	}
}
