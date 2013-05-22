using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlTypes;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public class Folder : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore, Ignore]
		public int? ItemId { get { return FolderId; } set { FolderId = ItemId; } }

		[JsonIgnore, Ignore]
		public ItemType ItemType { get { return ItemType.Folder; } }

		[JsonIgnore, Ignore]
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

		[JsonProperty("artId"), Ignore]
		public int? ArtId { get { return Art.ArtIdForItemId(FolderId); } }

		[JsonIgnore, Ignore]
		public string ArtPath { get { return FindArtPath(); } }

		/// <summary>
		/// Constructors
		/// </summary>

		public Folder()
		{

		}

		public Folder(IDataReader reader)
		{
			if ((object)reader == null)
			{
				return;
			}

			SetPropertiesFromQueryReader(reader);
		}

		public Folder ParentFolder()
		{
			return new Folder.Factory().CreateFolder((int)ParentFolderId);
		}

		public void Scan()
		{
			// TO DO: scanning!  yay!
		}

		public void SetPropertiesFromQueryReader(IDataReader reader)
		{
			if ((object)reader == null)
			{
				return;
			}
			
			FolderId = reader.GetInt32(reader.GetOrdinal("FolderId"));
			FolderName = reader.GetString(reader.GetOrdinal("FolderName"));
			FolderPath = reader.GetString(reader.GetOrdinal("FolderPath"));
			if (reader.GetValue(reader.GetOrdinal("ParentFolderId")) == DBNull.Value)
			{
				ParentFolderId = null;
			}
			else 
			{
				ParentFolderId = reader.GetInt32(reader.GetOrdinal("ParentFolderId"));
			}
			
			if (reader.GetValue(reader.GetOrdinal("MediaFolderId")) == DBNull.Value)
			{
				MediaFolderId = null;
			}
			else 
			{
				MediaFolderId = reader.GetInt32(reader.GetOrdinal("ParentFolderId"));
			}
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
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name, genre.genre_name FROM song " +
													 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
													 "LEFT JOIN album ON song_album_id = album.album_id " +
													 "LEFT JOIN genre ON song_genre_id = genre.genre_id " +
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
				logger.Error(e);
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
				logger.Error(e);
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
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				List<Folder> folders = conn.Query<Folder>("SELECT FolderId FROM folder WHERE ParentFolderId = ?", FolderId);
				folders.Sort(CompareFolderByName);
				return folders;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}
			
			return new List<Folder>();
		}

		public bool IsMediaFolder()
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
				}
			}

			if ((object)artPath == null)
			{
				// Check for any images
				Folder.ContainsImages(FolderPath, out artPath);
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
			
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();

				FolderId = itemId;
				if (!isMediaFolder)
				{
					ParentFolderId = GetParentFolderId(FolderPath);
				}

				conn.InsertLogged(this);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}
		}

		public int? GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			int? pFolderId = null;

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int id = conn.ExecuteScalar<int>("SELECT FolderId FROM folder WHERE FolderPath = ?", new object[] { parentFolderPath });

				if (id == 0)
				{
					if (logger.IsInfoEnabled) logger.Info("No db result for parent folder.  Constructing parent folder object.");
					Folder f = new Folder.Factory().CreateFolder(parentFolderPath);
					f.InsertFolder(false);
					pFolderId = f.FolderId;
				}
				else
				{
					pFolderId = id;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return pFolderId;
		}

		public static List<Folder> MediaFolders()
		{
			if (Settings.MediaFolders == null) 
			{
				ISQLiteConnection conn = null;
				try 
				{
					conn = Database.GetSqliteConnection();
					return conn.Query<Folder>("SELECT * FROM folder WHERE ParentFolderId IS NULL");
				} 
				catch (Exception e) 
				{
					logger.Info ("Failed reading list of media folders : " + e);
				} 
				finally
				{
					conn.Close();
				}
			} 
			else
			{
				return Settings.MediaFolders;
			}

			return new List<Folder>();
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

		/*
		 * Factory class
		 */

		public class Factory
		{
			private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

			public Folder CreateFolder(int folderId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();

					List<Folder> folder = conn.Query<Folder>("SELECT * FROM folder WHERE FolderId = ? LIMIT 1", new object[] { folderId });
					if (folder.Count > 0)
					{
						return folder[0];
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return null;
			}

			public Folder CreateFolder(string path)
			{
				Folder folder = new Folder();

				if (path == null || path == "")
				{
					return folder;
				}

				folder.FolderPath = path;
				folder.FolderName = Path.GetFileName(path);

				foreach (Folder mf in Folder.MediaFolders())
				{
					if (path.Contains(mf.FolderPath))
					{
						folder.MediaFolderId = mf.FolderId;
					}
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					if (folder.IsMediaFolder() || Settings.MediaFolders == null)
					{
						int folderId = conn.ExecuteScalar<int>("SELECT FolderId FROM folder WHERE FolderName = ? AND ParentFolderId IS NULL", new object[] { folder.FolderName });
						folder.FolderId = folderId == 0 ? (int?)null : folderId;
					}
					else
					{
						folder.ParentFolderId = folder.GetParentFolderId(folder.FolderPath);

						int folderId = conn.ExecuteScalar<int>("SELECT FolderId FROM folder WHERE FolderName = ? AND ParentFolderId = ?", new object[] { folder.FolderName, folder.ParentFolderId });
						folder.FolderId = folderId == 0 ? (int?)null : folderId;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return folder;
			}

			public Folder CreateFolder(string path, bool mediafolder)
			{
				if (path == null || path == "")
				{
					// No path so just return a folder
					return new Folder();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					IList<Folder> result = conn.Query<Folder>("SELECT * FROM folder WHERE FolderPath = ? AND MediaFolderId IS NULL", new object[] { path });

					foreach (Folder f in result)
					{
						if (path.Equals(f.FolderPath))
						{
							return f;
						}
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				// If not in database, return a folder object with the specified parameters
				Folder folder = new Folder();
				folder.FolderPath = path;
				folder.FolderName = Path.GetFileName(path);
				return folder;
			}
		}
	}
}
