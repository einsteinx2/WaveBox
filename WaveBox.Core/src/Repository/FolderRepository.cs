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
			{
				throw new ArgumentNullException("database");
			}
			if (serverSettings == null)
			{
				throw new ArgumentNullException("serverSettings");
			}
			if (songRepository == null)
			{
				throw new ArgumentNullException("songRepository");
			}
			if (videoRepository == null)
			{
				throw new ArgumentNullException("videoRepository");
			}

			this.database = database;
			this.serverSettings = serverSettings;
			this.songRepository = songRepository;
			this.videoRepository = videoRepository;
		}

		public Folder FolderForId(int folderId)
		{
			return this.database.GetSingle<Folder>(
				"SELECT Folder.*, ArtItem.ArtId FROM Folder " +
				"LEFT JOIN ArtItem ON Folder.FolderId = ArtItem.ItemId " +
				"WHERE FolderId = ? LIMIT 1",
			folderId);
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

			if (folder.IsMediaFolder() || serverSettings.MediaFolders == null)
			{
				int folderId = this.database.GetScalar<int>("SELECT FolderId FROM Folder WHERE FolderName = ? AND ParentFolderId IS NULL", folder.FolderName);
				folder.FolderId = folderId == 0 ? (int?)null : folderId;
			}
			else
			{
				folder.ParentFolderId = GetParentFolderId(folder.FolderPath);

				int folderId = this.database.GetScalar<int>("SELECT FolderId FROM Folder WHERE FolderName = ? AND ParentFolderId = ?", folder.FolderName, folder.ParentFolderId);
				folder.FolderId = folderId == 0 ? (int?)null : folderId;
			}

			return folder;
		}

		public IList<Folder> MediaFolders()
		{
			return this.database.GetList<Folder>("SELECT * FROM Folder WHERE ParentFolderId IS NULL");
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
			return this.database.GetList<Folder>("SELECT * FROM Folder WHERE ParentFolderId = ? ORDER BY FolderName COLLATE NOCASE", folderId);
		}

		public int? GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			int? pFolderId = null;

			int id = this.database.GetScalar<int>("SELECT FolderId FROM Folder WHERE FolderPath = ?", parentFolderPath);

			if (id == 0)
			{
				logger.IfInfo("No db result for parent folder.	Constructing parent folder object.");
				Folder f = FolderForPath(parentFolderPath);
				f.InsertFolder(false);
				pFolderId = f.FolderId;
			}
			else
			{
				pFolderId = id;
			}

			return pFolderId;
		}

		public IList<Album> AlbumsForFolderId(int folderId)
		{
			return this.database.GetList<Album>(
				"SELECT Album.*, AlbumArtist.AlbumArtistName, ArtItem.ArtId FROM Song " +
				"LEFT JOIN Folder ON Song.FolderId = Folder.FolderId " +
				"LEFT JOIN Album ON Song.AlbumId = Album.AlbumId " +
				"LEFT JOIN AlbumArtist ON AlbumArtist.AlbumArtistId = Album.AlbumArtistId " +
				"LEFT JOIN ArtItem ON Album.AlbumId = ArtItem.ItemId " +
				"WHERE Song.FolderId = ? GROUP BY Album.AlbumId ORDER BY Album.AlbumName COLLATE NOCASE",
			folderId);
		}
	}
}
