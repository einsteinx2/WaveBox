using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.Static;
using System.Security.Cryptography;
using Newtonsoft.Json;

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

		public Playlist(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		public Playlist(int playlistId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM playlist WHERE playlist_id = @playlistid", conn);
				q.AddNamedParam("@playlistid", playlistId);

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

		public Playlist(string playlistName)
		{
			PlaylistName = playlistName;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM playlist WHERE playlist_name = @playlistname", conn);
				q.AddNamedParam("@playlistname", playlistName);

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

		// Private methods

		private void SetPropertiesFromQueryReader(IDataReader reader)
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
				logger.Error(e);
			}
		}

		public string CalculateHash()
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			string itemIds = "";

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid", conn);
				q.AddNamedParam("@playlistid", PlaylistId);
				
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					itemIds += Convert.ToString(reader.GetInt32(reader.GetOrdinal("item_id")));
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

			return itemIds.MD5();
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
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = null;
				//q.Parameters.AddWithValue("@playlistid", PlaylistId);
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
					q = Database.GetDbCommand("INSERT INTO playlist (playlist_id, playlist_name, playlist_count, playlist_duration, md5_hash, last_update) " +
											"VALUES (@playlistid, @playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)", conn);
					//q = Database.GetDbCommand("INSERT INTO playlist VALUES (@playlistid, @playlistname, @playlistcount, @playlistduration, @md5, @lastupdate)");
					//q.Parameters.AddWithValue("@playlistid", DBNull.Value);
				}
				else
				{
					q = Database.GetDbCommand("UPDATE playlist SET playlist_name = @playlistname, "
						+ "playlist_count = @playlistcount, "
						+ "playlist_duration = @playlistduration, "
						+ "md5_hash = @md5, "
						+ "last_update = @lastupdate "
						+ "WHERE playlist_id = @playlistid", conn);
				}

				if (PlaylistName == null)
				{
					q.AddNamedParam("@playlistname", "");
				}
				else
				{
					q.AddNamedParam("@playlistname", PlaylistName);
				}

				q.AddNamedParam("@playlistcount", PlaylistCount);
				q.AddNamedParam("@playlistduration", PlaylistDuration);
				q.AddNamedParam("@md5", PlaylistId == 0 ? "" : CalculateHash());
				q.AddNamedParam("@lastupdate", LastUpdateTime);
				q.AddNamedParam("@playlistid", PlaylistId);

				q.Prepare();
				q.ExecuteNonQueryLogged();
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

		public int IndexOfMediaItem(IMediaItem item)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			int index = 0;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT item_position FROM playlist_item " + 
					"WHERE playlist_id = @playlistid AND item_type_id = @itemtypeid " + 
					"ORDER BY item_position LIMIT 1", conn);
				q.AddNamedParam("@playlistid", PlaylistId);
				q.AddNamedParam("@itemtypeid", item.ItemTypeId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					index = reader.GetInt32(reader.GetOrdinal("item_position"));
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

			return index;
		}

		public IMediaItem MediaItemAtIndex(int index)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			IMediaItem item = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition", conn);
				q.AddNamedParam("@playlistid", PlaylistId);
				q.AddNamedParam("@itemposition", index);

				q.Prepare();
				reader = q.ExecuteReader();


				if (reader.Read())
				{
					int itemid = reader.GetInt32(reader.GetOrdinal("item_id"));
					int itemtypeid = reader.GetInt32(reader.GetOrdinal("item_type_id"));
					ItemType it = ItemTypeExtensions.ItemTypeForId(itemtypeid);

					switch (it)
					{
						case ItemType.Song:
							item = new Song.Factory().CreateSong(itemid);
							break;
						case ItemType.Video:
							// nothing for now
							break;
						default:
							break;
					}
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

			return item;
		}

		public List<IMediaItem> ListOfMediaItems()
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			List<IMediaItem> items = new List<IMediaItem>();

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM playlist_item WHERE playlist_id = @playlistid ORDER BY item_position", conn);
				q.AddNamedParam("@playlistid", PlaylistId);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					int itemid = reader.GetInt32(reader.GetOrdinal("item_id"));
					switch (reader.GetInt32(reader.GetOrdinal("item_type_id")))
					{
						case (int)ItemType.Song:
						items.Add(new Song.Factory().CreateSong(itemid));
							break;
						case (int)ItemType.Video:
							// nothing for now
							break;
						default:
							break;
					}
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

			return items;
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

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition", conn);
				q.AddNamedParam("@playlistid", PlaylistId);
				q.AddNamedParam("@itemposition", index);

				q.Prepare();
				q.ExecuteNonQueryLogged();

				IDbCommand q1 = Database.GetDbCommand("UPDATE playlist_item SET item_position = item_position - 1 WHERE playlist_id = @playlistid AND item_position > @item_position", conn);
				q1.AddNamedParam("@playlistid", PlaylistId);
				q1.AddNamedParam("@item_position", index);
				q1.Prepare();
				q1.ExecuteNonQueryLogged();
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

		public void RemoveMediaItemAtIndexes(List<int> indices)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			IDbTransaction trans = null;

			try
			{
				conn = Database.GetDbConnection();
				trans = conn.BeginTransaction();
				IDbCommand q = Database.GetDbCommand("", conn);

				// temporary storage for playlist item information.  We can't use temp tables with SQL CE, so this
				// is a workaround for that.  There is probably a better solution.
				ArrayList idValues = new ArrayList();


				// delete the items at the indicated indices
				foreach (int index in indices)
				{
					q.CommandText = "DELETE FROM playlist_item WHERE playlist_id = @playlistid AND item_position = @itemposition";
					q.AddNamedParam("@playlistid", PlaylistId);
					q.AddNamedParam("@itemposition", index);

					q.Prepare();
					q.ExecuteNonQueryLogged();
				}

				// select the id of all members of the playlist
				q.CommandText = "SELECT playlist_item_id FROM playlist_item WHERE playlist_id = @playlistid";
				q.AddNamedParam("@playlistid", PlaylistId);

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
					//q.CommandText = "SELECT playlist_item_id FROM playlist_item WHERE playlist_id = @playlistid";
					q.CommandText = "UPDATE playlist_item SET playlist_item_id = @newid WHERE playlist_item_id = @oldid AND playlist_id = @playlistid";
					q.AddNamedParam("@newid", i + 1);
					q.AddNamedParam("@oldid", (int)idValues[i]);
					q.AddNamedParam("@playlistid", PlaylistId);

					q.Prepare();
					q.ExecuteNonQueryLogged();
				}

				trans.Commit();
			}
			catch (Exception e)
			{
				if (trans != null)
				{
					trans.Rollback();
				}
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
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

			IDbConnection conn = null;
			IDataReader reader = null;
			IDbTransaction trans = null;

			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				conn = Database.GetDbConnection();
				trans = conn.BeginTransaction();
				IDbCommand q = Database.GetDbCommand("UPDATE playlist_item SET item_position = item_position + 1 " + 
					"WHERE playlist_id = @playlistid AND item_position >= @itemposition", conn);
				q.AddNamedParam("@playlistid", PlaylistId);
				q.AddNamedParam("@itemposition", toIndex);

				q.Prepare();
				q.ExecuteNonQueryLogged();

				// conditional rollback here

				// If the fromIndex is higher than toIndex, compensate for the position update above
				fromIndex = fromIndex < toIndex ? fromIndex : fromIndex - 1;

				IDbCommand q1 = Database.GetDbCommand("UPDATE playlist_item SET item_position = @toitemposition " + 
					"WHERE playlist_id = @playlistid AND item_position = @fromitemposition", conn);

				q1.AddNamedParam("@toitemposition", toIndex);
				q1.AddNamedParam("@playlistid", PlaylistId);
				q1.AddNamedParam("@fromitemposition", fromIndex);
				q1.Prepare();
				int affectedRows = q1.ExecuteNonQueryLogged();

				if (affectedRows != 1)
				{
					trans.Rollback();
					return;
				}

				trans.Commit();
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

		public void AddMediaItem(IMediaItem item, bool updateDatabase)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				int? id = Item.GenerateItemId(ItemType.PlaylistItem);
				// to do - better way of knowing whether or not a query has been successfully completed.
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO playlist_item (playlist_item_id, playlist_id, item_type_id, item_id, item_position) VALUES " +
													"(@playlistitemid, @playlistid, @itemtypeid, @itemid, @itempos)", conn);
				q.AddNamedParam("@playlistitemid", id);
				q.AddNamedParam("@playlistid", PlaylistId);
				q.AddNamedParam("@itemtypeid", item.ItemTypeId);
				q.AddNamedParam("@itemid", item.ItemId);
				q.AddNamedParam("@itempos", PlaylistCount == null ? 0 : PlaylistCount);

				q.Prepare();
				q.ExecuteNonQueryLogged();
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
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				// to do - better way of knowing whether or not a query has been successfully completed.
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("DELETE FROM playlist_item WHERE playlist_id = @playlistid", conn); 
				q.AddNamedParam("@playlistid", PlaylistId);

				q.Prepare();
				q.ExecuteNonQueryLogged();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
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
	}
}
