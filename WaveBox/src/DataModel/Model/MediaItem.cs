using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class MediaItem
	{
		public virtual int ItemTypeId { get { return 0; } }

		[JsonProperty("itemId")]
		public int ItemId { get; set; }

		[JsonProperty("folderId")]
		public int? FolderId { get; set; }

		[JsonProperty("fileType")]
		public FileType FileType { get; set; }

		[JsonProperty("duration")]
		public int Duration { get; set; }

		[JsonProperty("bitrate")]
		public int Bitrate { get; set; }

		[JsonProperty("fileSize")]
		public long FileSize { get; set; }

		[JsonProperty("lastModified")]
		public long LastModified { get; set; }
		
		[JsonProperty("fileName")]
		public string FileName { get; set; }


		/// <summary>
		/// Public methods
		/// </summary>

		public void AddToPlaylist(Playlist thePlaylist, int index)
		{
		}

		public static bool FileNeedsUpdating(FileInfo file, int? folderId)
		{
            // We don't need to instantiate another folder to know what the folder id is.  This should be known when the method is called.

			//var sw = new Stopwatch();
			string fileName = file.Name;
			long lastModified = Convert.ToInt64(file.LastWriteTime.Ticks);
			bool needsUpdating = true;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
                // Turns out that COUNT(*) on large tables is REALLY slow in SQLite because it does a full table search.  I created an index on folder_id(because weirdly enough,
                // even though it's a primary key, SQLite doesn't automatically make one!  :O).  We'll pull that, and if we get a row back, then we'll know that this thing exists.

				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song_id FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod", conn);
                //IDbCommand q = Database.GetDbCommand("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod", conn);

                q.AddNamedParam("@folderid", folderId);
				q.AddNamedParam("@filename", fileName);
				q.AddNamedParam("@lastmod", lastModified);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					needsUpdating = false;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[MEDIAITEM(1)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return needsUpdating;
		}

		public string FilePath()
		{
			return new Folder(FolderId).FolderPath + Path.DirectorySeparatorChar + FileName;
		}

		public FileStream File()
		{
			return new FileStream(FilePath(), FileMode.Open, FileAccess.Read);
		}
	}
}
