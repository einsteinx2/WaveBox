using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Model.Repository;

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
		public int? ArtId { get { return Injection.Kernel.Get<IArtRepository>().ArtIdForItemId(FolderId); } }

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

		public List<Song> ListOfSongs(bool recursive = false)
		{
			if (FolderId == null)
				return new List<Song>();

			return Injection.Kernel.Get<IFolderRepository>().ListOfSongs((int)FolderId, recursive);
		}

		public List<Video> ListOfVideos(bool recursive = false)
		{
			if (FolderId == null)
				return new List<Video>();

			return Injection.Kernel.Get<IFolderRepository>().ListOfVideos((int)FolderId, recursive);
		}

		public List<Folder> ListOfSubFolders()
		{
			if (FolderId == null)
				return new List<Folder>();

			return Injection.Kernel.Get<IFolderRepository>().ListOfSubFolders((int)FolderId);
		}

		public bool IsMediaFolder()
		{
			Folder mFolder = MediaFolder();

			if (mFolder != null)
			{
				return true;
			}
			
			return false;
		}

		private Folder MediaFolder()
		{
			foreach (Folder mediaFolder in Injection.Kernel.Get<IFolderRepository>().MediaFolders())
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
			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Folder);
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
					ParentFolderId = Injection.Kernel.Get<IFolderRepository>().GetParentFolderId(FolderPath);
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

				foreach (Folder mf in Injection.Kernel.Get<IFolderRepository>().MediaFolders())
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
						folder.ParentFolderId = Injection.Kernel.Get<IFolderRepository>().GetParentFolderId(folder.FolderPath);

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
