using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Community.CsharpSqlite.SQLiteClient;
using Community.CsharpSqlite;
using WaveBox.DataModel.Singletons;

namespace WaveBox.DataModel.Model
{
	public class Playlist
	{
		public int PlaylistId { get; set; }

		public string PlaylistName { get; set; }

		public int PlaylistCount { get; set; }

		public int PlaylistDuration { get; set; }

		public string Md5Hash { get; set; }

		public long LastUpdateTime { get; set; }


		public Playlist()
		{
		}

		public Playlist(SqliteDataReader reader)
		{
			SetPropertiesFromQueryResult(reader);
		}

		public Playlist(int playlistId)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM playlist WHERE playlist_id = @playlistid");
					q.Parameters.Add("@playlistid", playlistId);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		public Playlist(string playlistName)
		{
			PlaylistName = playlistName;

			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM playlist WHERE playlist_name = @playlistname");
					q.Parameters.Add("@playlistname", playlistName);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		// Private methods

		private void SetPropertiesFromQueryResult(SqliteDataReader reader)
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

		public string Md5OfString(string input)
		{
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input)), 0);
		}

		// what is the synchronized keyword in java?
		public string CalculateHash()
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			string itemIds = "";

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid");
					q.Parameters.Add("@playlistid", PlaylistId);

					conn = Database.GetDbConnection();
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
					Database.Close(conn, reader);
				}
			}

			return Md5OfString(itemIds);
		}


		public void UpdateProperties(int itemsAdded, int durationAdded)
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

		public void UpdateDatabase()
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					SqliteCommand q;
					//q.Parameters.Add("@playlistid", PlaylistId);
					if (PlaylistId == 0)
					{
						q = new SqliteCommand("INSERT INTO playlist (playlist_name, playlist_count, playlist_duration, md5_hash, last_update) VALUES (@playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)");
						//q = new SqliteCommand("INSERT INTO playlist VALUES (@playlistid, @playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)");
						//q.Parameters.Add("@playlistid", DBNull.Value);
					}
					else
					{
						q = new SqliteCommand("UPDATE playlist SET playlist_name = @playlistname, "
							+ "playlist_count = @playlistcount, "
							+ "playlist_duration = @playlistduration, "
							+ "md5_hash = @md5, "
							+ "last_update = @lastupdate "
							+ "WHERE playlist_id = @playlistid"
						);

						q.Parameters.Add("@playlistid", PlaylistId);
					}

					if (PlaylistName == null)
						q.Parameters.Add("@playlistname", "");
					else
						q.Parameters.Add("@playlistname", PlaylistName);

					q.Parameters.Add("@playlistcount", PlaylistCount);
					q.Parameters.Add("@playlistduration", PlaylistDuration);
					q.Parameters.Add("@md5", PlaylistId == 0 ? "" : CalculateHash());
					q.Parameters.Add("@lastupdate", LastUpdateTime);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					q.ExecuteNonQuery();

					if (PlaylistId == 0)
					{
						q.CommandText = "SELECT last_insert_rowid()";
						PlaylistId = Convert.ToInt32(q.ExecuteScalar().ToString());
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		public int IndexOfMediaItem(MediaItem item)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			int index = 0;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT item_position FROM playlist_item " + 
						"WHERE playlist_id = @playlistid AND item_type_id = @itemtypeid " + 
						"ORDER BY item_position LIMIT 1"
					);
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemid", item.ItemId);
					q.Parameters.Add("@itemtypeid", item.ItemTypeId);

					conn = Database.GetDbConnection();
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
					Database.Close(conn, reader);
				}
			}

			return index;
		}

		public MediaItem MediaItemAtIndex(int index)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			MediaItem item = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition");
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemposition", index);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();


					if (reader.Read())
					{
						var itemid = reader.GetInt32(reader.GetOrdinal("item_id"));
						int itemtypeid = reader.GetInt32(reader.GetOrdinal("item_type_id"));
						ItemType it = ItemTypeExtensions.ItemTypeForId(itemtypeid);

						switch (it)
						{
						case ItemType.SONG:
							item = new Song(itemid);
							break;

						case ItemType.VIDEO:
								// nothing for now
							break;

						default:
							break;
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			return item;
		}

		public List<MediaItem> ListOfMediaItems()
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			List<MediaItem> items = new List<MediaItem>();

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid ORDER BY item_position");
					q.Parameters.Add("@playlistid", PlaylistId);

					conn = Database.GetDbConnection();
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

							default:
								break;
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			return items;
		}

		public void RemoveMediaItem(MediaItem item)
		{
			RemoveMediaItemAtIndex(IndexOfMediaItem(item));
		}

		public void RemoveMediaItems(List<MediaItem> items)
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

			RemoveMediaItemAtIndexes(indexes);
		}

		public void RemoveMediaItemAtIndex(int index)
		{
			if (PlaylistId == 0)
			{
				return;
			}

			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition");
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemposition", index);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					q.ExecuteNonQuery();

					q.CommandText = "UPDATE playlist_item SET item_position = item_position - 1";
					q.CommandText += "WHERE playlist_id = @playlistid AND item_position > @item_position";
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemposition", index);
					q.Prepare();
					q.ExecuteNonQuery();
				}
				catch (Exception e)
				{
					Console.WriteLine("[PLAYLIST] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		public void RemoveMediaItemAtIndexes(List<int> indices)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			SqliteTransaction trans = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					trans = (SqliteTransaction)conn.BeginTransaction();
					var q = new SqliteCommand();
					q.Connection = conn;

					// temporary storage for playlist item information.  We can't use temp tables with SQL CE, so this
					// is a workaround for that.  There is probably a better solution.
					ArrayList idValues = new ArrayList();


					// delete the items at the indicated indices
					foreach (int index in indices)
					{
						q.CommandText = "DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition";
						q.Parameters.Add("@playlistid", PlaylistId);
						q.Parameters.Add("@itemposition", index);

						q.Prepare();
						q.ExecuteNonQuery();
					}

					// select the id of all members of the playlist
					q.CommandText = "SELECT playlist_item_id FROM playlist_item WHERE playlist_id = @playlistid";
					q.Parameters.Add("@playlistid", PlaylistId);

					q.Prepare();
					reader = q.ExecuteReader();

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
						q.Parameters.Add("@newid", i + 1);
						q.Parameters.Add("@oldid", (int)idValues[i]);
						q.Parameters.Add("@playlistid", PlaylistId);

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
					Database.Close(conn, reader);
				}
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

			SqliteConnection conn = null;
			SqliteDataReader reader = null;
			SqliteTransaction trans = null;

			lock (Database.dbLock)
			{
				try
				{
					// to do - better way of knowing whether or not a query has been successfully completed.
					trans = (SqliteTransaction)conn.BeginTransaction();
					var q = new SqliteCommand("UPDATE playlist_item SET item_position = item_position + 1 " + 
						"WHERE playlist_id = @playlistid AND item_position >= @itemposition"
					);
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemposition", toIndex);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					q.ExecuteNonQuery();

					// conditional rollback here

					// If the fromIndex is higher than toIndex, compensate for the position update above
					fromIndex = fromIndex < toIndex ? fromIndex : fromIndex - 1;

					var q1 = new SqliteCommand("UPDATE playlist_item SET item_position = @toitemposition " + 
						"WHERE playlist_id = @playlistid AND item_position = @fromitemposition"
					);

					q1.Parameters.Add("@toitemposition", toIndex);
					q1.Parameters.Add("@playlistid", PlaylistId);
					q1.Parameters.Add("@fromitemposition", fromIndex);
					q1.Prepare();
					var res1 = q1.ExecuteNonQuery();

					if (res1 != 1)
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
					Database.Close(conn, reader);
				}
			}
		}

		public void AddMediaItem(MediaItem item, bool updateDatabase)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					// to do - better way of knowing whether or not a query has been successfully completed.
					var q = new SqliteCommand("INSERT INTO playlist_item (playlist_id, item_type_id, item_id, item_position) VALUES (@playlistid, @itemtypeid, @itemid, @itempos)");
					q.Parameters.Add("@nullid", DBNull.Value);
					q.Parameters.Add("@playlistid", PlaylistId);
					q.Parameters.Add("@itemtypeid", item.ItemTypeId);
					q.Parameters.Add("@itemid", item.ItemId);
					q.Parameters.Add("@itempos", PlaylistCount);

					conn = Database.GetDbConnection();
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
					Database.Close(conn, reader);
				}
			}
		}

		public void AddMediaItems(List<MediaItem> items)
		{
			if(PlaylistId == 0 || items == null)
			{
				return;
			}

			int duration = 0;
			foreach(MediaItem item in items)
			{
				AddMediaItem(item, false);
				duration += item.Duration;
			}

			UpdateProperties(items.Count, duration);
			UpdateDatabase();
		}

		public void InsertMediaItem(MediaItem item, int index)
		{
		}

		public void ClearPlaylist()
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					// to do - better way of knowing whether or not a query has been successfully completed.
					var q = new SqliteCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid"); 
					q.Parameters.Add("@playlistid", PlaylistId);

					conn = Database.GetDbConnection();
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
					Database.Close(conn, reader);
				}
			}
		}

		public void createPlaylist()
		{
			UpdateDatabase();
		}

		public void deletePlaylist()
		{
		}

	}
}
