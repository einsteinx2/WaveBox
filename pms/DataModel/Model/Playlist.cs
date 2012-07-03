using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using MediaFerry.DataModel.Singletons;

namespace MediaFerry.DataModel.Model
{
	public class Playlist
	{
		private int _playlistId;
		public int PlaylistId
		{
			get
			{
				return _playlistId;
			}

			set
			{
				_playlistId = value;
			}
		}

		private string _playlistName;
		public string PlaylistName
		{
			get
			{
				return _playlistName;
			}

			set
			{
				_playlistName = value;
			}
		}

		private int _playlistCount;
		public int PlaylistCount
		{
			get
			{
				return _playlistCount;
			}

			set
			{
				_playlistCount = value;
			}
		}

		private int _playlistDuration;
		public int PlaylistDuration
		{
			get
			{
				return _playlistDuration;
			}

			set
			{
				_playlistDuration = value;
			}
		}

		private string _md5Hash;
		public string Md5Hash
		{
			get
			{
				return _md5Hash;
			}

			set
			{
				_md5Hash = value;
			}
		}

		private long _lastUpdateTime;
		public long LastUpdateTime
		{
			get
			{
				return _lastUpdateTime;
			}

			set
			{
				_lastUpdateTime = value;
			}
		}

		public Playlist()
		{
		}

		public Playlist(SqlCeDataReader reader)
		{
			_setPropertiesFromQueryResult(reader);
		}

		public Playlist(int playlistId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM playlist WHERE playlist_id = @playlistid");
				q.Parameters.AddWithValue("@playlistid", playlistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public Playlist(string playlistName)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM playlist WHERE playlist_name = @playlistname");
				q.Parameters.AddWithValue("@playlistname", playlistName);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		// Private methods

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			try
			{
				PlaylistId = reader.GetInt32(reader.GetOrdinal("playlist_id"));
				PlaylistName = reader.GetString(reader.GetOrdinal("playlist_name"));
				PlaylistCount = reader.GetInt32(reader.GetOrdinal("playlist_count"));
				PlaylistDuration = reader.GetInt32(reader.GetOrdinal("playlist_duration"));
				Md5Hash = reader.GetString(reader.GetOrdinal("md5_hash"));
				LastUpdateTime = reader.GetInt32(reader.GetOrdinal("last_update"));
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		public string _md5OfString(string input)
		{
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input)), 0);
		}

		// what is the synchronized keyword in java?
		public string _calculateHash()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			string itemIds = "";

			try
			{
				var q = new SqlCeCommand("SELECT * FROM playlist WHERE playlist_id = @playlistid");
				q.Parameters.AddWithValue("@playlistname", PlaylistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					itemIds += reader.GetString(reader.GetOrdinal("item_id"));
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return _md5OfString(itemIds);
		}


		public void _updateProperties(int itemsAdded, int durationAdded)
		{
			// update playlist count
			int playlistCount = PlaylistCount;
			PlaylistCount = playlistCount + itemsAdded;

			// update last update time
			LastUpdateTime = ((DateTime.Now.Ticks / 10) - (new DateTime(1970, 1, 1).Ticks / 10));	// correct?

			// update playlist duration
			int playlistDuration = PlaylistDuration;
			PlaylistDuration = (playlistDuration + durationAdded);
		}

		public void _updateDatabase()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("INSERT INTO playlist VALUES (@playlistid, @playlistname, @playlistcount, @playlistduration, @md5, @lastupdate");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@playlistname", PlaylistName);
				q.Parameters.AddWithValue("@playlistcount", PlaylistCount);
				q.Parameters.AddWithValue("@playlistduration", PlaylistDuration);
				q.Parameters.AddWithValue("@md5", Md5Hash);
				q.Parameters.AddWithValue("@lastupdate", LastUpdateTime);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public int indexOfMediaItem(MediaItem item)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			int index = 0;

			try
			{
				var q = new SqlCeCommand("SELECT item_position FROM playlist_item " + 
										 "WHERE playlist_id = @playlistid AND item_type_id = @itemtypeid " + 
										 "ORDER BY item_position LIMIT 1");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemid", item.ItemId);
				q.Parameters.AddWithValue("@itemtypeid", item.ItemTypeId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					index = reader.GetInt32(reader.GetOrdinal("item_position"));
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return index;
		}

		public MediaItem mediaItemAtIndex(int index)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			MediaItem item = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemposition", index);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					var itemid = reader.GetInt32(reader.GetOrdinal("item_id"));
					switch (reader.GetInt32(reader.GetOrdinal("item_type_id")))
					{
						case (int)ItemType.SONG:
							item = new Song(itemid);
							break;

						case (int)ItemType.VIDEO:
							// nothing for now
							break;

						default: break;
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return item;
		}

		public List<MediaItem> listOfMediaItems()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			List<MediaItem> items = new List<MediaItem>();

			try
			{
				var q = new SqlCeCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid ORDER BY item_position");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					var itemid = reader.GetInt32(reader.GetOrdinal("item_id"));
					switch (reader.GetInt32(reader.GetOrdinal("item_type_id")))
					{
						case (int)ItemType.SONG:
							items.Add(new Song(itemid));
							break;

						case (int)ItemType.VIDEO:
							// nothing for now
							break;

						default: break;
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return items;
		}

		public void removeMediaItem(MediaItem item)
		{
			removeMediaItemAtIndex(indexOfMediaItem(item));
		}

		public void removeMediaItems(List<MediaItem> items)
		{
			var indexes = new List<int>();
			if (PlaylistId == null || items == null)
			{
				return;
			}

			foreach (MediaItem item in items)
			{
				indexes.Add(item.ItemId);
			}

			removeMediaItemAtIndexes(indexes);
		}

		public void removeMediaItemAtIndex(int index)
		{
			if (PlaylistId == null)
			{
				return;
			}

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemposition", index);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();

				q.CommandText =  "UPDATE playlist_item SET item_position = item_position - 1";
				q.CommandText += "WHERE playlist_id = @playlistid AND item_position > @item_position";
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemposition", index);
				q.Prepare();
				q.ExecuteNonQuery();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void removeMediaItemAtIndexes(List<int> indexes)
		{
		}

		public void moveMediaItem(int fromIndex, int toIndex)
		{
		}

		public void addMediaItem(MediaItem item, bool updateDatabase)
		{
		}

		public void addMediaItems(List<MediaItem> items)
		{
		}

		public void insertMediaItem(MediaItem item, int index)
		{
		}

		public void clearPlaylist()
		{
		}

		public void createPlaylist()
		{
		}

		public void deletePlaylist()
		{
		}

	}
}
