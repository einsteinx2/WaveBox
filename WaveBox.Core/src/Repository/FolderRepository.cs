using System;
using System.Collections.Generic;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class FolderRepository : IFolderRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IServerSettings serverSettings;
		private readonly ISongRepository songRepository;
		private readonly IVideoRepository videoRepository;

		public FolderRepository(IDatabase database, IServerSettings serverSettings, ISongRepository songRepository, IVideoRepository videoRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (serverSettings == null)
				throw new ArgumentNullException("serverSettings");
			if (songRepository == null)
				throw new ArgumentNullException("songRepository");
			if (videoRepository == null)
				throw new ArgumentNullException("videoRepository");

			this.database = database;
			this.serverSettings = serverSettings;
			this.songRepository = songRepository;
			this.videoRepository = videoRepository;
		}

		public Folder FolderForId(int folderId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

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
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public Folder FolderForPath(string path)
		{
			Folder folder = new Folder();

			if (path == null || path == "")
			{
				return folder;
			}

			folder.FolderPath = path;
			folder.FolderName = Path.GetFileName(path);

			foreach (Folder mf in MediaFolders())
			{
				if (path.Contains(mf.FolderPath))
				{
					folder.MediaFolderId = mf.FolderId;
				}
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				if (folder.IsMediaFolder() || serverSettings.MediaFolders == null)
				{
					int folderId = conn.ExecuteScalar<int>("SELECT FolderId FROM Folder WHERE FolderName = ? AND ParentFolderId IS NULL", folder.FolderName);
					folder.FolderId = folderId == 0 ? (int?)null : folderId;
				}
				else
				{
					folder.ParentFolderId = GetParentFolderId(folder.FolderPath);

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
				database.CloseSqliteConnection(conn);
			}

			return folder;
		}

		public IList<Folder> MediaFolders()
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
					logger.IfInfo ("Failed reading list of media folders : " + e);
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

		public IList<Folder> TopLevelFolders()
		{
			List<Folder> folders = new List<Folder>();

			foreach (Folder mediaFolder in MediaFolders())
			{
				folders.AddRange(mediaFolder.ListOfSubFolders());
			}

			folders.Sort(Folder.CompareFolderByName);
			return folders;
		}

		public IList<Song> ListOfSongs(int folderId, bool recursive = false)
		{
			var listOfSongs = new List<Song>();

			// Recursively add media in all subfolders to the list.
			listOfSongs.AddRange(songRepository.SearchSongs("FolderId", folderId.ToString()));

			if (recursive == true)
			{
				foreach (var subf in ListOfSubFolders(folderId))
				{
					listOfSongs.AddRange(subf.ListOfSongs(true));
				}
			}

			return listOfSongs;
		}

		public IList<Video> ListOfVideos(int folderId, bool recursive = false)
		{
			var listOfVideos = new List<Video>();

			// Recursively add media in all subfolders to the list.
			listOfVideos.AddRange(videoRepository.SearchVideos("FolderId", folderId.ToString()));

			if (recursive == true)
			{
				foreach (var subf in ListOfSubFolders(folderId))
				{
					listOfVideos.AddRange(subf.ListOfVideos(true));
				}
			}

			return listOfVideos;
		}

		public IList<Folder> ListOfSubFolders(int folderId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
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
				conn = database.GetSqliteConnection();
				int id = conn.ExecuteScalar<int>("SELECT FolderId FROM Folder WHERE FolderPath = ?", parentFolderPath);

				if (id == 0)
				{
					logger.IfInfo("No db result for parent folder.  Constructing parent folder object.");
					Folder f = FolderForPath(parentFolderPath);
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
				database.CloseSqliteConnection(conn);
			}

			return pFolderId;
		}
	}
}

