using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlTypes;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.IO;
using NLog;

namespace WaveBox.DataModel.Model
{
	public class Folder : IItem
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		[JsonIgnore]
		public int? ItemId { get { return FolderId; } set { FolderId = ItemId; } }

		[JsonIgnore]
		public ItemType ItemType { get { return ItemType.Folder; } }

		[JsonIgnore]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("folderId")]
		public int? FolderId { get; set; }

		[JsonProperty("folderName")]
		public string FolderName { get; set; }

		[JsonProperty("parentFolderId")]
		public int? ParentFolderId { get; set; }

		[JsonProperty("mediaFolderId")]
		public int? MediaFolderId { get; set; }

		[JsonProperty("folderPath")]
		public string FolderPath { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get { return Art.ArtIdForItemId(FolderId); } }

		[JsonIgnore]
		public string ArtPath { get { return FindArtPath(); } }

		/// <summary>
		/// Constructors
		/// </summary>

		public Folder(int? folderId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("SELECT * FROM folder " + 
					"WHERE folder_id = @folderid LIMIT 1", conn);

				q.AddNamedParam("@folderid", folderId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					FolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
					FolderName = reader.GetString(reader.GetOrdinal("folder_name"));
					FolderPath = reader.GetString(reader.GetOrdinal("folder_path"));
					if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
					{
						ParentFolderId = null;
					}
					else 
					{
						ParentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
					}

					if (reader.GetValue(reader.GetOrdinal("folder_media_folder_id")) == DBNull.Value)
					{
						MediaFolderId = null;
					}
					else 
					{
						MediaFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(1)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Folder(string path)
		{
			IDbConnection conn = null;
			IDbCommand q = null;
			IDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			FolderPath = path;
			FolderName = Path.GetFileName(path);

			foreach (Folder mf in MediaFolders())
			{
				if (path.Contains(mf.FolderPath))
				{
					MediaFolderId = mf.FolderId;
				}
			}

			try
			{
				conn = Database.GetDbConnection();
				if (IsMediaFolder() || Settings.MediaFolders == null)
				{
					q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id IS NULL", conn);
					q.AddNamedParam("@foldername", FolderName);
				}
				else
				{
					ParentFolderId = GetParentFolderId(FolderPath);
					q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id = @parentfolderid", conn);
					q.AddNamedParam("@foldername", FolderName);
					q.AddNamedParam("@parentfolderid", ParentFolderId);
				}

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					FolderId = reader.GetInt32(0);
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(2)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}
		

		public Folder(string path, bool mediafolder)
		{
			FolderPath = path;
			FolderName = Path.GetFileName(path);
			ParentFolderId = null;
			MediaFolderId = null;
			FolderId = null;

			IDbCommand q = null;
			IDbConnection conn = null;
			IDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			try
			{
				conn = Database.GetDbConnection();
				q = Database.GetDbCommand("SELECT * FROM folder WHERE folder_path = \"" + path + "\" AND folder_media_folder_id IS NULL", conn);
				//q = Database.GetDbCommand("SELECT * FROM folder WHERE folder_path = @folderpath AND folder_media_folder_id = 0", conn);
				//q.AddNamedParam("@folderpath", path);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					if (path == reader.GetString(reader.GetOrdinal("folder_path")))
					{
						FolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
						FolderName = reader.GetString(reader.GetOrdinal("folder_name"));
						FolderName = reader.GetString(reader.GetOrdinal("folder_path"));

						if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
						{
							ParentFolderId = null;
						}
						else
						{
							ParentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
						}

						if (reader.GetValue(reader.GetOrdinal("folder_media_folder_id")) == DBNull.Value)
						{
							MediaFolderId = null;
						}
						else
						{
							MediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));
						}
					}
				}
				reader.Close();
				conn.Close();
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(3)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Folder ParentFolder()
		{
			return new Folder(ParentFolderId);
		}

		public void Scan()
		{
			// TO DO: scanning!  yay!
		}

		public List<IMediaItem> ListOfMediaItems()
		{
			List<IMediaItem> mediaItems = new List<IMediaItem>();

			mediaItems.AddRange(ListOfSongs());
			mediaItems.AddRange(ListOfVideos());

			return mediaItems;
		}

		public List<Song> ListOfSongs()
		{
			List<Song> songs = new List<Song>();
			
			IDbConnection conn = null;
			IDataReader reader = null;
			
			// For now just get songs
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
													 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
													 "LEFT JOIN album ON song_album_id = album.album_id " +
													 "WHERE song_folder_id = @folderid", conn);
				
				q.AddNamedParam("@folderid", FolderId);
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					songs.Add(new Song(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(4)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			songs.Sort(Song.CompareSongsByDiscAndTrack);
			
			return songs;
		}

		public List<Video> ListOfVideos()
		{
			List<Video> videos = new List<Video>();

			IDbConnection conn = null;
			IDataReader reader = null;
			
			// For now just get songs
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM video " +
													 "WHERE video_folder_id = @folderid", conn);
				
				q.AddNamedParam("@folderid", FolderId);
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					videos.Add(new Video(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(4)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			videos.Sort(Video.CompareVideosByFileName);
			
			return videos;
		}

		public List<Folder> ListOfSubFolders()
		{
			List<Folder> folders = new List<Folder>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE parent_folder_id = @folderid", conn);
				q.AddNamedParam("@folderid", FolderId);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					folders.Add(new Folder(reader.GetInt32(0)));
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(5)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			folders.Sort(CompareFolderByName);
			return folders;
		}

		private bool IsMediaFolder()
		{
			Folder mFolder = MediaFolder();

			if (mFolder != null)
			{
				return true;
			}
			else return false;
		}

		public static bool ContainsImages(string dir, out string firstImageFoundPath)
		{
			string[] validImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
			string ext = null;
			firstImageFoundPath = null;

			foreach (string file in Directory.GetFiles(dir))
			{
				ext = Path.GetExtension(file).ToLower();
				if (validImageExtensions.Contains(ext) && !Path.GetFileName(file).StartsWith("."))
				{
					firstImageFoundPath = file;
				}
			}

			// Return true if firstImageFoundPath exists
			return ((object)firstImageFoundPath != null);
		}

		private string FindArtPath()
		{
			string artPath = null;

			foreach (string fileName in Settings.FolderArtNames)
			{
				string path = FolderPath + Path.DirectorySeparatorChar + fileName;
				if (File.Exists(path))
				{
					// Use this one
					artPath = path;
					//logger.Info("Found standard art for folder " + FolderName + " - file name: " + fileName);
				}
			}

			if ((object)artPath == null)
			{
				// Check for any images
				Folder.ContainsImages(FolderPath, out artPath);

				/*if ((object)artPath == null)
				{
					logger.Info("folder " + FolderName + " contains no images");
				}
				else
				{
					logger.Info("Found non-standard art for folder " + FolderName + " - path: " + artPath);
				}*/
			}

			return artPath;
		}

		private Folder MediaFolder()
		{
			foreach (Folder mediaFolder in Folder.MediaFolders())
			{
				if (FolderPath == mediaFolder.FolderPath)
				{
					return mediaFolder;
				}
			}

			return null;
		}

		public void InsertFolder(bool isMediaFolder)
		{
			int? itemId = Item.GenerateItemId(ItemType.Folder);
			if (itemId == null)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			int affected;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO folder (folder_id, folder_name, folder_path, parent_folder_id, folder_media_folder_id) " +
													 "VALUES (@folderid, @foldername, @folderpath, @parentfolderid, @mediafolderid)", conn);
				q.AddNamedParam("@folderid", itemId);
				q.AddNamedParam("@foldername", FolderName);
				q.AddNamedParam("@folderpath", FolderPath);

				if (isMediaFolder == true)
				{
					q.AddNamedParam("@parentfolderid", DBNull.Value);
				}
				else
				{
					q.AddNamedParam("@parentfolderid", GetParentFolderId(FolderPath));
				}

				q.AddNamedParam("@mediafolderid", MediaFolderId);
				
				q.Prepare();
				affected = q.ExecuteNonQueryLogged();

				if (affected > 0)
				{
					// get the id of the previous insert.  weird.
					FolderId = itemId;
					//FolderId = Convert.ToInt32(((SqlDecimal)q.ExecuteScalar()).ToString());
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(6)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		private int? GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			IDbConnection conn = null;
			IDataReader reader = null;
			int? pFolderId = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_path = @folderpath", conn);
				q.AddNamedParam("@folderpath", parentFolderPath);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					pFolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
				}
				else
				{
					logger.Info("No db result for parent folder.  Constructing parent folder object.");
					Folder f = new Folder(parentFolderPath);
					f.InsertFolder(false);
					pFolderId = f.FolderId;
				}
			}
			catch (Exception e)
			{
				logger.Error("[FOLDER(7)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return pFolderId;
		}

		public static List<Folder> MediaFolders ()
		{
			List<Folder> folders = new List<Folder> ();
			IDbConnection conn = null;
			IDataReader reader = null;

			if (Settings.MediaFolders == null) 
			{
				try 
				{
					conn = Database.GetDbConnection();
					IDbCommand q = Database.GetDbCommand ("SELECT * FROM folder WHERE parent_folder_id IS NULL", conn);
					//q.Parameters.AddWithValue("@nullvalue", DBNull.Value);
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						folders.Add (new Folder(reader.GetInt32(reader.GetOrdinal("folder_id"))));
					}
				} 
				catch (Exception e) 
				{
					logger.Info ("[FOLDER(8)] " + e);
				} 
				finally
				{
					Database.Close(conn, reader);
				}
			} 
			else
			{
				return Settings.MediaFolders;
			}

			return folders;
		}

		public static List<Folder> TopLevelFolders()
		{
			List<Folder> folders = new List<Folder>();

			foreach (Folder mediaFolder in MediaFolders())
			{
				folders.AddRange(mediaFolder.ListOfSubFolders());
			}

			folders.Sort(CompareFolderByName);
			return folders;
		}

		public static int CompareFolderByName(Folder x, Folder y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.FolderName, y.FolderName);
		}
	}
}
