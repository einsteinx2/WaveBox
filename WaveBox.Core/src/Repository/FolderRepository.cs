using System;
using WaveBox.Core.Injection;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using Ninject;
using System.IO;

namespace WaveBox.Model.Repository
{
	public class FolderRepository : IFolderRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IServerSettings serverSettings;

		public FolderRepository(IDatabase database, IServerSettings serverSettings)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (serverSettings == null)
				throw new ArgumentNullException("serverSettings");

			this.database = database;
			this.serverSettings = serverSettings;
		}

		public List<Folder> MediaFolders()
		{
			if (serverSettings.MediaFolders == null) 
			{
				ISQLiteConnection conn = null;
				try 
				{
					conn = database.GetSqliteConnection();
					return conn.Query<Folder>("SELECT * FROM Folder WHERE ParentFolderId IS NULL");
				} 
				catch (Exception e) 
				{
					logger.Info ("Failed reading list of media folders : " + e);
				} 
				finally
				{
					database.CloseSqliteConnection(conn);
				}
			} 
			else
			{
				return serverSettings.MediaFolders;
			}

			return new List<Folder>();
		}

		public List<Folder> TopLevelFolders()
		{
			List<Folder> folders = new List<Folder>();

			foreach (Folder mediaFolder in MediaFolders())
			{
				folders.AddRange(mediaFolder.ListOfSubFolders());
			}

			folders.Sort(Folder.CompareFolderByName);
			return folders;
		}

		public List<Song> ListOfSongs(int folderId, bool recursive = false)
		{
			var listOfSongs = new List<Song>();

			// Recursively add media in all subfolders to the list.
			listOfSongs.AddRange(Injection.Kernel.Get<ISongRepository>().SearchSongs("FolderId", folderId.ToString()));

			if (recursive == true)
			{
				foreach (var subf in ListOfSubFolders(folderId))
				{
					listOfSongs.AddRange(subf.ListOfSongs(true));
				}
			}

			return listOfSongs;
		}

		public List<Video> ListOfVideos(int folderId, bool recursive = false)
		{
			var listOfVideos = new List<Video>();

			// Recursively add media in all subfolders to the list.
			listOfVideos.AddRange(Injection.Kernel.Get<IVideoRepository>().SearchVideos("FolderId", folderId.ToString()));

			if (recursive == true)
			{
				foreach (var subf in ListOfSubFolders(folderId))
				{
					listOfVideos.AddRange(subf.ListOfVideos(true));
				}
			}

			return listOfVideos;
		}

		public List<Folder> ListOfSubFolders(int folderId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				List<Folder> folders = conn.Query<Folder>("SELECT * FROM Folder WHERE ParentFolderId = ?", folderId);
				folders.Sort(Folder.CompareFolderByName);
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
	}
}

