using System;
using WaveBox.Core.Injection;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;

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
	}
}

