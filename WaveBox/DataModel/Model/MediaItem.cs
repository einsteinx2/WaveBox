using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class MediaItem
	{
		public virtual int ItemTypeId
		{
			get
			{
				return 0;
			}
		}

		protected MediaItemType _mediaItemType;
		[JsonProperty("mediaItemType")]
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
		[JsonProperty("itemId")]
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
		[JsonProperty("artId")]
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
		[JsonProperty("folderId")]
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
		[JsonProperty("fileType")]
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
		[JsonProperty("duration")]
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

		protected int _bitrate;
		[JsonProperty("bitrate")]
		public int Bitrate
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
		[JsonProperty("fileSize")]
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
		[JsonProperty("lastModified")]
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
		[JsonProperty("fileName")]
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
			//var sw = new Stopwatch();
			//sw.Start();
			//Console.WriteLine("Checking to see if file needs updating: " + file.Name);
			int folderId = new Folder(file.Directory.ToString()).FolderId;
			string fileName = file.Name;
			long lastModified = Convert.ToInt64(file.LastWriteTime.Ticks);
			bool needsUpdating = true;
			//sw.Stop();

			//Console.WriteLine("Get file information: {0} ms", sw.ElapsedMilliseconds);
			//sw.Reset();

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			try
			{
				//sw.Start();
				var q = new SQLiteCommand("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod");
				q.Parameters.AddWithValue("@folderid", folderId);
				q.Parameters.AddWithValue("@filename", fileName);
				q.Parameters.AddWithValue("@lastmod", lastModified);
				//sw.Stop();
				//Console.WriteLine("Add parameters: {0} ms", sw.ElapsedMilliseconds);
				//sw.Reset();

				//sw.Start();
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				//sw.Stop();
				//Console.WriteLine("Get db connection: {0} ms", sw.ElapsedMilliseconds);
				//sw.Reset();

				//sw.Start();
				q.Connection = conn;
				q.Prepare();
				int i = (int)q.ExecuteScalar();

				if (i >= 1)
				{
					needsUpdating = false;
				}
				//sw.Stop();
				//Console.WriteLine("Do query: {0} ms; count is {1}", sw.ElapsedMilliseconds, i);
				//sw.Reset();
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
			return needsUpdating;
		}

		public FileStream file()
		{
			return new FileStream(new Folder(FolderId).FolderPath + Path.DirectorySeparatorChar + FileName, FileMode.Open, FileAccess.Read);
		}
	}
}
