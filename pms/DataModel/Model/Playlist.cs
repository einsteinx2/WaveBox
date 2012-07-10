using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using MediaFerry.DataModel.Singletons;

namespace MediaFerry.DataModel.Model
{
	public class Playlist
	{
		private Object sync = new Object();

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
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public Playlist(string playlistName)
		{
			PlaylistName = playlistName;

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
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
				LastUpdateTime = reader.GetInt64(reader.GetOrdinal("last_update"));
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
				var q = new SqlCeCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					itemIds += Convert.ToString(reader.GetInt32(reader.GetOrdinal("item_id")));
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
				SqlCeCommand q;
				//q.Parameters.AddWithValue("@playlistid", PlaylistId);
				if (PlaylistId == 0)
				{
					q = new SqlCeCommand("INSERT INTO playlist (playlist_name, playlist_count, playlist_duration, md5_hash, last_update) VALUES (@playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)");
					//q = new SqlCeCommand("INSERT INTO playlist VALUES (@playlistid, @playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)");
					//q.Parameters.AddWithValue("@playlistid", DBNull.Value);
				}

				else
				{
					q = new SqlCeCommand("UPDATE playlist SET playlist_name = @playlistname, "
										+ "playlist_count = @playlistcount, "
										+ "playlist_duration = @playlistduration, "
										+ "md5_hash = @md5, "
										+ "last_update = @lastupdate "
										+ "WHERE playlist_id = @playlistid");

					q.Parameters.AddWithValue("@playlistid", PlaylistId);
				}

				if (PlaylistName == null)
					 q.Parameters.AddWithValue("@playlistname", "");
				else q.Parameters.AddWithValue("@playlistname", PlaylistName);

				q.Parameters.AddWithValue("@playlistcount", PlaylistCount);
				q.Parameters.AddWithValue("@playlistduration", PlaylistDuration);
				q.Parameters.AddWithValue("@md5", PlaylistId == 0 ? "" : _calculateHash());
				q.Parameters.AddWithValue("@lastupdate", LastUpdateTime);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();

				if (PlaylistId == 0)
				{
					q.CommandText = "SELECT @@IDENTITY";
					PlaylistId = Convert.ToInt32(q.ExecuteScalar().ToString());
				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
					int itemtypeid = reader.GetInt32(reader.GetOrdinal("item_type_id"));
					ItemType it = ItemTypeExtensions.itemTypeForId(itemtypeid);

					switch (it)
					{
						case ItemType.SONG:
							item = new Song(itemid);
							break;

						case ItemType.VIDEO:
							// nothing for now
							break;

						default: break;
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
				Console.WriteLine("[PLAYLIST] " + e.ToString());
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
			if (PlaylistId == 0 || items == null)
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
			if (PlaylistId == 0)
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
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void removeMediaItemAtIndexes(List<int> indices)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			SqlCeTransaction trans = null;

			try
			{
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				trans = conn.BeginTransaction();
				var q = new SqlCeCommand();
				q.Connection = conn;

				// temporary storage for playlist item information.  We can't use temp tables with SQL CE, so this
				// is a workaround for that.  There is probably a better solution.
				ArrayList idValues = new ArrayList();
				

				// delete the items at the indicated indices
				foreach (int index in indices)
				{
					q.CommandText = "DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition";
					q.Parameters.AddWithValue("@playlistid", PlaylistId);
					q.Parameters.AddWithValue("@itemposition", index);

					q.Prepare();
					q.ExecuteNonQuery();
				}

				// select the id of all members of the playlist
				q.CommandText = "SELECT playlist_item_id FROM playlist_item WHERE playlist_id = @playlistid";
				q.Parameters.AddWithValue("@playlistid", PlaylistId);

				q.Prepare();
				reader =  q.ExecuteReader();

				// insert them into an array
				while (reader.Read())
				{
					idValues.Add(reader.GetInt32(0));
				}

				// update the values of each index in the array to be the new index
				for (int i = 0; i < idValues.Count; i++)
				{
					q.CommandText = "SELECT playlist_item_id FROM playlist_item WHERE playlist_id = @playlistid";
					q.CommandText = "UPDATE playlist_item SET playlist_item_id = @newid WHERE playlist_item_id = @oldid AND playlist_id = @playlistid";
					q.Parameters.AddWithValue("@newid", i + 1);
					q.Parameters.AddWithValue("@oldid", (int)idValues[i]);
					q.Parameters.AddWithValue("@playlistid", PlaylistId);

					q.Prepare();
					q.ExecuteNonQuery();
				}

				trans.Commit();
			}

			catch (Exception e)
			{
				if (trans != null)
				{
					trans.Rollback();
				}
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void moveMediaItem(int fromIndex, int toIndex)
		{
			// make sure the input is within bounds and is not null
			if (fromIndex == 0 || toIndex == 0 ||
				fromIndex > PlaylistCount || fromIndex < 0 ||
				toIndex < 0 || toIndex == fromIndex)
			{
				return;
			}

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			SqlCeTransaction trans = null;

			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				trans = conn.BeginTransaction();
				var q = new SqlCeCommand("UPDATE playlist_item SET item_position = item_position + 1 " + 
										 "WHERE playlist_id = @playlistid AND item_position >= @itemposition");
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemposition", toIndex);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();

				// conditional rollback here

				// If the fromIndex is higher than toIndex, compensate for the position update above
				fromIndex = fromIndex < toIndex ? fromIndex : fromIndex - 1;

				var q1 = new SqlCeCommand("UPDATE playlist_item SET item_position = @toitemposition " + 
								"WHERE playlist_id = @playlistid AND item_position = @fromitemposition");

				q1.Parameters.AddWithValue("@toitemposition", toIndex);
				q1.Parameters.AddWithValue("@playlistid", PlaylistId);
				q1.Parameters.AddWithValue("@fromitemposition", fromIndex);
				q1.Prepare();
				var res1 = q1.ExecuteNonQuery();

				if(res1 != 1)
				{
					trans.Rollback();
					return;
				}

				trans.Commit();
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void addMediaItem(MediaItem item, bool updateDatabase)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				var q = new SqlCeCommand("INSERT INTO playlist_item (playlist_id, item_type_id, item_id, item_position) VALUES (@playlistid, @itemtypeid, @itemid, @itempos)");
				q.Parameters.AddWithValue("@nullid", DBNull.Value);
				q.Parameters.AddWithValue("@playlistid", PlaylistId);
				q.Parameters.AddWithValue("@itemtypeid", item.ItemTypeId);
				q.Parameters.AddWithValue("@itemid", item.ItemId);
				q.Parameters.AddWithValue("@itempos", PlaylistCount);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void addMediaItems(List<MediaItem> items)
		{
			if(PlaylistId == 0 || items == null)
			{
				return;
			}

			int duration = 0;
			foreach(MediaItem item in items)
			{
				addMediaItem(item, false);
				duration += item.Duration;
			}

			_updateProperties(items.Count, duration);
			_updateDatabase();
		}

		public void insertMediaItem(MediaItem item, int index)
		{
		}

		public void clearPlaylist()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				var q = new SqlCeCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid"); 
				q.Parameters.AddWithValue("@playlistid", PlaylistId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				q.ExecuteNonQuery();
			}

			catch (Exception e)
			{
				Console.WriteLine("[PLAYLIST] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void createPlaylist()
		{
			_updateDatabase();
		}

		public void deletePlaylist()
		{
		}

	}
}
