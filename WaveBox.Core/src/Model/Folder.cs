using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model
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
			return Injection.Kernel.Get<IFolderRepository>().FolderForId((int)ParentFolderId);
		}

		public void Scan()
		{
			// TO DO: scanning!  yay!
		}

		public IList<IMediaItem> ListOfMediaItems()
		{
			List<IMediaItem> mediaItems = new List<IMediaItem>();

			mediaItems.AddRange(ListOfSongs());
			mediaItems.AddRange(ListOfVideos());

			return mediaItems;
		}

		public IList<Song> ListOfSongs(bool recursive = false)
		{
			if (FolderId == null)
				return new List<Song>();

			return Injection.Kernel.Get<IFolderRepository>().ListOfSongs((int)FolderId, recursive);
		}

		public IList<Video> ListOfVideos(bool recursive = false)
		{
			if (FolderId == null)
				return new List<Video>();

			return Injection.Kernel.Get<IFolderRepository>().ListOfVideos((int)FolderId, recursive);
		}

		public IList<Folder> ListOfSubFolders()
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
	}
}
