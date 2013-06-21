using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class Playlist : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore]
		public int? ItemId { get { return PlaylistId; } set { PlaylistId = ItemId; } }
		
		[JsonIgnore]
		public ItemType ItemType { get { return ItemType.Playlist; } }
		
		[JsonIgnore]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("id")]
		public int? PlaylistId { get; set; }
	
		[JsonProperty("name")]
		public string PlaylistName { get; set; }

		[JsonProperty("count")]
		public int? PlaylistCount { get; set; }

		[JsonProperty("duration")]
		public int? PlaylistDuration { get; set; }

		[JsonProperty("md5Hash")]
		public string Md5Hash { get; set; }

		[JsonProperty("lastUpdateTime")]
		public long? LastUpdateTime { get; set; }


		public Playlist()
		{
		}

		public string CalculateHash()
		{
			StringBuilder itemIds = new StringBuilder();

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.Query<PlaylistItem>("SELECT ItemId FROM PlaylistItem WHERE PlaylistId = ?", PlaylistId);
				
				foreach (PlaylistItem playlistItem in result)
				{
					itemIds.Append(playlistItem.ItemId);
					itemIds.Append("|");
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

			return itemIds.ToString().MD5();
		}

		public void UpdateProperties(int itemsAdded, int durationAdded)
		{
			// update playlist count
			int? playlistCount = PlaylistCount;
			PlaylistCount = playlistCount + itemsAdded;

			// update last update time
			LastUpdateTime = DateTime.Now.ToUniversalUnixTimestamp() - (new DateTime(1970, 1, 1).ToUniversalUnixTimestamp());

			// update playlist duration
			int? playlistDuration = PlaylistDuration;
			PlaylistDuration = (playlistDuration + durationAdded);
		}

		public void UpdateDatabase()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

				if (PlaylistId == null)
				{
					int? itemId = Item.GenerateItemId(ItemType.Playlist);
					if (itemId == null)
					{
						return;
					}

					PlaylistId = itemId;
					PlaylistCount = 0;
					PlaylistDuration = 0;
					LastUpdateTime = DateTime.Now.ToUniversalUnixTimestamp() - (new DateTime(1970, 1, 1).ToUniversalUnixTimestamp());

					conn.ExecuteLogged("INSERT INTO Playlist (PlaylistId, PlaylistName, PlaylistCount, PlaylistDuration, Md5Hash, LastUpdateTime) " +
					                   "VALUES (?, ?, ?, ?, ?, ?)", PlaylistId, PlaylistName == null ? "" : PlaylistName, PlaylistCount, PlaylistDuration, CalculateHash(), LastUpdateTime);
				}
				else
				{
					conn.ExecuteLogged("UPDATE Playlist SET PlaylistName = ?, PlaylistCount = ?, PlaylistDuration = ?, Md5Hash = ?, LastUpdateTime = ? " +
					                   "WHERE PlaylistId = ?", PlaylistName == null ? "" : PlaylistName, PlaylistCount, PlaylistDuration, PlaylistId == 0 ? "" : CalculateHash(), LastUpdateTime, PlaylistId);
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
		}

		public int IndexOfMediaItem(IMediaItem item)
		{
			ISQLiteConnection conn = null;

			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT ItemPosition FROM PlaylistItem " + 
				                               "WHERE PlaylistId = ? AND ItemType = ? " + 
				                               "ORDER BY ItemPosition LIMIT 1", PlaylistId, item.ItemTypeId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return 0;
		}

		public IMediaItem MediaItemAtIndex(int index)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<PlaylistItem>("SELECT * FROM PlaylistItem WHERE PlaylistId = ? AND ItemPosition = ? LIMIT 1", PlaylistId, index);

				foreach (PlaylistItem playlistItem in result)
				{
					switch (playlistItem.ItemType)
					{
						case ItemType.Song: return new Song.Factory().CreateSong((int)playlistItem.ItemId); 
						case ItemType.Video: return new Video.Factory().CreateVideo((int)playlistItem.ItemId);
						default: break;
					}
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

			return new MediaItem();
		}

		public List<IMediaItem> ListOfMediaItems()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<PlaylistItem>("SELECT * FROM PlaylistItem WHERE PlaylistId = ? ORDER BY ItemPosition", PlaylistId);

				var items = new List<IMediaItem>();
				foreach (PlaylistItem playlistItem in result)
				{
					switch (playlistItem.ItemType)
					{
						case ItemType.Song: items.Add(new Song.Factory().CreateSong((int)playlistItem.ItemId)); break;
						case ItemType.Video: items.Add(new Video.Factory().CreateVideo((int)playlistItem.ItemId)); break;
						default: break;
					}
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

			return new List<IMediaItem>();
		}

		public void RemoveMediaItem(IMediaItem item)
		{
			RemoveMediaItemAtIndex(IndexOfMediaItem(item));
		}

		public void RemoveMediaItems(List<IMediaItem> items)
		{
			List<int> indexes = new List<int>();
			if (PlaylistId == 0 || items == null)
			{
				return;
			}

			foreach (IMediaItem item in items)
			{
				if (item.ItemId != null)
				{
					indexes.Add((int)item.ItemId);
				}
			}

			RemoveMediaItemAtIndexes(indexes);
		}

		public void RemoveMediaItemAtIndex(int index)
		{
			if (PlaylistId == 0)
			{
				return;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.BeginTransaction();
				conn.ExecuteLogged("DELETE FROM PlaylistItem WHERE PlaylistId = ? AND ItemPosition = ?", PlaylistId, index);
				conn.ExecuteLogged("UPDATE PlaylistItem SET ItemPosition = ItemPosition - 1 WHERE PlaylistId = ? AND ItemPosition > ?", PlaylistId, index);
				conn.Commit();
			}
			catch (Exception e)
			{
				if (!ReferenceEquals(conn, null))
				{
					conn.Rollback();
				}
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public void RemoveMediaItemAtIndexes(List<int> indices)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.BeginTransaction();

				// delete the items at the indicated indices
				foreach (int index in indices)
				{
					conn.ExecuteLogged("DELETE FROM PlaylistItem WHERE PlaylistId = ? AND ItemPosition = ?", PlaylistId, index);
				}

				// select the id of all members of the playlist
				var result = conn.Query<PlaylistItem>("SELECT * FROM PlaylistItem WHERE PlaylistId = ?", PlaylistId);

				// update the values of each index in the array to be the new index
				for (int i = 0; i < result.Count; i++) 
				{
					var item = result[i];

					conn.ExecuteLogged("UPDATE playlist_item SET PlaylistItemId = ? WHERE PlaylistItemId = ? AND PlaylistId = ?", i + 1, item.PlaylistItemId, PlaylistId);
				}

				conn.Commit();
			}
			catch (Exception e)
			{
				if (!ReferenceEquals(conn, null))
				{
					conn.Rollback();
				}
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public void MoveMediaItem(int fromIndex, int toIndex)
		{
			// make sure the input is within bounds and is not null
			if (fromIndex == 0 || toIndex == 0 ||
				fromIndex > PlaylistCount || fromIndex < 0 ||
				toIndex < 0 || toIndex == fromIndex)
			{
				return;
			}

			ISQLiteConnection conn = null;
			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.BeginTransaction();
				conn.ExecuteLogged("UPDATE PlaylistItem SET ItemPosition = ItemPosition + 1 WHERE PlaylistId = ? AND ItemPosition >= ?", PlaylistId, toIndex);

				// conditional rollback here

				// If the fromIndex is higher than toIndex, compensate for the position update above
				fromIndex = fromIndex < toIndex ? fromIndex : fromIndex - 1;

				conn.ExecuteLogged("UPDATE PlaylistItem SET ItemPosition = ? WHERE PlaylistId = ? AND ItemPosition = ?", toIndex, PlaylistId, fromIndex);

				// conditional rollback here

				conn.Commit();
			}
			catch (Exception e)
			{
				if (!ReferenceEquals(conn, null))
				{
					conn.Rollback();
				}
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public void AddMediaItem(IMediaItem item, bool updateDatabase)
		{
			ISQLiteConnection conn = null;
			try
			{
				int? id = Item.GenerateItemId(ItemType.PlaylistItem);
				// to do - better way of knowing whether or not a query has been successfully completed.
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var playlistItem = new PlaylistItem();
				playlistItem.PlaylistItemId = id;
				playlistItem.PlaylistId = PlaylistId;
				playlistItem.ItemType = item.ItemType;
				playlistItem.ItemId = item.ItemId;
				playlistItem.ItemPosition = PlaylistCount == null ? 0 : PlaylistCount;
				conn.Insert(playlistItem);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public void AddMediaItems(List<IMediaItem> items)
		{
			if (PlaylistId == 0 || items == null)
			{
				return;
			}

			int duration = 0;
			foreach (IMediaItem item in items)
			{
				AddMediaItem(item, false);

				duration += (item.Duration == null ? 0 : (int)item.Duration);
			}

			UpdateProperties(items.Count, duration);
			UpdateDatabase();
		}

		public void InsertMediaItem(IMediaItem item, int index)
		{

		}

		public void ClearPlaylist()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				conn.ExecuteLogged("DELETE FROM PlaylistItem WHERE PlaylistId = ?", PlaylistId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			PlaylistCount = 0;
			PlaylistDuration = 0;
			UpdateDatabase();
		}

		public void CreatePlaylist()
		{
			UpdateDatabase();
		}

		public void DeletePlaylist()
		{

		}

		public class Factory
		{
			public Playlist CreatePlaylist(int playlistId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Playlist>("SELECT * FROM Playlist WHERE PlaylistId = ?", playlistId);

					foreach (Playlist p in result)
					{
						return p;
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

				return new Playlist();
			}

			public Playlist CreatePlaylist(string playlistName)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Playlist>("SELECT * FROM Playlist WHERE PlaylistName = ?", playlistName);

					foreach (Playlist p in result)
					{
						return p;
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

				Playlist playlist = new Playlist();
				playlist.PlaylistName = playlistName;
				return playlist;
			}
		}
	}
}
