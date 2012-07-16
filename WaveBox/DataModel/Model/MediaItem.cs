using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Data.Sqlite;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class MediaItem
	{
		public virtual long ItemTypeId
		{
			get
			{
				return 0;
			}
		}

		[JsonProperty("mediaItemType")]
		public MediaItemType MediaItemType { get; set; }

		[JsonProperty("itemId")]
		public long ItemId { get; set; }

		[JsonProperty("artId")]
		public long ArtId { get; set; }

		[JsonProperty("folderId")]
		public long FolderId { get; set; }

		[JsonProperty("fileType")]
		public FileType FileType { get; set; }

		[JsonProperty("duration")]
		public long Duration { get; set; }

		[JsonProperty("bitrate")]
		public long Bitrate { get; set; }

		[JsonProperty("fileSize")]
		public long FileSize { get; set; }

		[JsonProperty("lastModified")]
		public long LastModified { get; set; }
		
		[JsonProperty("fileName")]
		public string FileName { get; set; }


		/// <summary>
		/// Public methods
		/// </summary>

		public void AddToPlaylist(Playlist thePlaylist, long index)
		{
		}

		public static bool FileNeedsUpdating(FileInfo file)
		{
			//var sw = new Stopwatch();
			//sw.Start();
			//Console.WriteLine("Checking to see if file needs updating: " + file.Name);
			long folderId = new Folder(file.Directory.ToString()).FolderId;
			string fileName = file.Name;
			long lastModified = Convert.ToInt64(file.LastWriteTime.Ticks);
			bool needsUpdating = true;
			//sw.Stop();

			//Console.WriteLine("Get file information: {0} ms", sw.ElapsedMilliseconds);
			//sw.Reset();

			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					//sw.Start();
					var q = new SqliteCommand("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod");
					q.Parameters.AddWithValue("@folderid", folderId);
					q.Parameters.AddWithValue("@filename", fileName);
					q.Parameters.AddWithValue("@lastmod", lastModified);
					//sw.Stop();
					//Console.WriteLine("Add parameters: {0} ms", sw.ElapsedMilliseconds);
					//sw.Reset();

					//sw.Start();
					conn = Database.GetDbConnection();
					//sw.Stop();
					//Console.WriteLine("Get db connection: {0} ms", sw.ElapsedMilliseconds);
					//sw.Reset();

					//sw.Start();
					q.Connection = conn;
					q.Prepare();
					long i = (long)q.ExecuteScalar();

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
					Database.Close(conn, reader);
				}
			}
			return needsUpdating;
		}

		public FileStream File()
		{
			return new FileStream(new Folder(FolderId).FolderPath + Path.DirectorySeparatorChar + FileName, FileMode.Open, FileAccess.Read);
		}
	}
}
