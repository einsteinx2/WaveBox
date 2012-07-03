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
		}

		public void _updateDatabase()
		{
		}

		public int indexOfMediaItem(MediaItem item)
		{
			return 1;
		}

		public MediaItem mediaItemAtIndex(int index)
		{
			return new MediaItem();
		}

		public List<MediaItem> listOfMediaItems()
		{
			return new List<MediaItem>();
		}

		public void removeMediaItem(MediaItem item)
		{
		}

		public void removeMediaItemAtIndex(int index)
		{
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
