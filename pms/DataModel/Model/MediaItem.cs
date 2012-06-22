using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlServerCe;
using pms.DataModel.Model;
using pms.DataModel.Singletons;

namespace pms.DataModel.Model
{
	public class MediaItem
	{
		public int ItemTypeId
		{
			get
			{
				return 0;
			}
		}

		protected MediaItemType _mediaItemType;
		public MediaItemType MediaItemType
		{
			get
			{
				return _mediaItemType;
			}

			set
			{
				_mediaItemType = value;
			}
		}

		protected int _itemId;
		public int ItemId
		{
			get
			{
				return _itemId;
			}

			set
			{
				_itemId = value;
			}
		}

		protected int _artId;
		public int ArtId
		{
			get
			{
				return _artId;
			}

			set
			{
				_artId = value;
			}
		}

		protected int _folderId;
		public int FolderId
		{
			get
			{
				return _folderId;
			}

			set
			{
				_folderId = value;
			}
		}

		protected FileType _fileType;
		public FileType FileType
		{
			get
			{
				return _fileType;
			}

			set
			{
				_fileType = value;
			}
		}

		protected int _duration;
		public int Duration
		{
			get
			{
				return _duration;
			}

			set
			{
				_duration = value;
			}
		}

		protected long _bitrate;
		public long Bitrate
		{
			get
			{
				return _bitrate;
			}

			set
			{
				_bitrate = value;
			}
		}

		protected long _fileSize;
		public long FileSize
		{
			get
			{
				return _fileSize;
			}

			set
			{
				_fileSize = value;
			}
		}

		protected long _lastModified;
		public long LastModified
		{
			get
			{
				return _lastModified;
			}

			set
			{
				_lastModified = value;
			}
		}

		protected string _fileName;
		public string FileName
		{
			get
			{
				return _fileName;
			}

			set
			{
				_fileName = value;
			}
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public void addToPlaylist(Playlist thePlaylist, int index)
		{
		}

		public static bool fileNeedsUpdating(FileInfo file)
		{
			int folderId = new Folder(file.Directory.ToString()).FolderId;
			string fileName = file.Name;
			long lastModified = Convert.ToInt64(file.LastWriteTime);
			bool needsUpdating = true;

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				string query = string.Format("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = {0} AND song_file_name = {1} ANd song_last_modified = {2}",
					folderId, fileName, lastModified);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					if (reader.GetInt32(0) >= 1)
					{
						needsUpdating = false;
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}
			return needsUpdating;
		}
	}
}
