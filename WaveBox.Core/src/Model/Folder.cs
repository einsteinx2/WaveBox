using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.IO;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class Folder : IItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public int? ItemId { get { return FolderId; } set { FolderId = ItemId; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public ItemType ItemType { get { return ItemType.Folder; } }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
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

		[JsonProperty("artId"), IgnoreRead, IgnoreWrite]
		public int? ArtId { get { return Art.ArtIdForItemId(FolderId); } }

		/// <summary>
		/// Constructors
		/// </summary>

		public Folder()
		{

		}

		public Folder ParentFolder()
		{
			return new Folder.Factory().CreateFolder((int)ParentFolderId);
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
			return Song.SearchSongs("FolderId", FolderId.ToString());
		}

		public List<Video> ListOfVideos()
		{
			return Video.SearchVideos("FolderId", FolderId.ToString());
		}

		public List<Folder> ListOfSubFolders()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				List<Folder> folders = conn.Query<Folder>("SELECT * FROM Folder WHERE ParentFolderId = ?", FolderId);
				folders.Sort(CompareFolderByName);
				return folders;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public int? GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			int? pFolderId = null;

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int id = conn.ExecuteScalar<int>("SELECT FolderId FROM Folder WHERE FolderPath = ?", parentFolderPath);

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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return pFolderId;
		}

		public static List<Folder> MediaFolders()
		{
			IServerSettings serverSettings = Injection.Kernel.Get<IServerSettings>();

			if (serverSettings.MediaFolders == null) 
			{
				ISQLiteConnection conn = null;
				try 
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					return conn.Query<Folder>("SELECT * FROM Folder WHERE ParentFolderId IS NULL");
				} 
				catch (Exception e) 
				{
					logger.Info ("Failed reading list of media folders : " + e);
				} 
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}
			} 
			else
			{
				return serverSettings.MediaFolders;
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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

					List<Folder> folder = conn.Query<Folder>("SELECT * FROM Folder WHERE FolderId = ? LIMIT 1", folderId);
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
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					if (folder.IsMediaFolder() || Injection.Kernel.Get<IServerSettings>().MediaFolders == null)
					{
						int folderId = conn.ExecuteScalar<int>("SELECT FolderId FROM Folder WHERE FolderName = ? AND ParentFolderId IS NULL", folder.FolderName);
						folder.FolderId = folderId == 0 ? (int?)null : folderId;
					}
					else
					{
						folder.ParentFolderId = folder.GetParentFolderId(folder.FolderPath);

						int folderId = conn.ExecuteScalar<int>("SELECT FolderId FROM Folder WHERE FolderName = ? AND ParentFolderId = ?", folder.FolderName, folder.ParentFolderId);
						folder.FolderId = folderId == 0 ? (int?)null : folderId;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				return folder;
			}
		}
	}
}
