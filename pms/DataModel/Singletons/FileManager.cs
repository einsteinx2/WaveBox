using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using MediaFerry.DataModel.FolderScanning;
using System.IO;

namespace MediaFerry.DataModel.Singletons
{
	class FileManager
	{
		List<Folder> mf;
		private ScanQueue sq;
		//private List<FileSystemWatcher> watcherList;

		public FileManager()
		{
			mf = Settings.MediaFolders;
			sq = new ScanQueue();
			sq.startScanQueue();
			sq.queueOperation(new FolderScanning.OrphanScanOperation(0));

			foreach (var folder in mf)
			{
				sq.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

				// create watcher
				if(Directory.Exists(folder.FolderPath))
				{
					var watch = new FileSystemWatcher(folder.FolderPath);
					watch.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
					watch.Changed += new FileSystemEventHandler(OnChanged);
					watch.Created += new FileSystemEventHandler(OnCreated);
					watch.Deleted += new FileSystemEventHandler(OnDeleted);
					watch.Renamed += new RenamedEventHandler(OnRenamed);
				
					watch.IncludeSubdirectories = true;
					watch.EnableRaisingEvents = true;
					Console.WriteLine("[FILEMANAGER] File system watcher added for: {0}", folder.FolderPath);
					//watcherList.Add(watch);
				}

			}
		}

		private static FileManager instance;
		public static FileManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new FileManager();
				}

				return instance;
			}
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			// Specify what is done when a file is changed, created, or deleted.
			//Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
		}

		private void OnCreated(object source, FileSystemEventArgs e)
		{
			Console.WriteLine("[FILEMANAGER] File created: {0}", e.FullPath);
			if (File.Exists(e.FullPath))
			{
				var dir = new FileInfo(e.FullPath).DirectoryName;
				sq.queueOperation(new FolderScanOperation(dir, ScanQueue.DEFAULT_DELAY));
			}

			else if (Directory.Exists(e.FullPath))
			{
				sq.queueOperation(new FolderScanOperation(e.FullPath, ScanQueue.DEFAULT_DELAY));
			}
		}

		private void OnDeleted(object source, FileSystemEventArgs e)
		{
			// if a file got deleted, we need to remove the orphan from the db
			sq.queueOperation(new OrphanScanOperation(ScanQueue.DEFAULT_DELAY));
		}

		private void OnRenamed(object source, RenamedEventArgs e)
		{
			// if a file is renamed, its db entry is probably orphaned.  remove the orphan and
			// add the renamed file as a new entry
			Console.WriteLine("[FILEMANAGER] {0} renamed to {1}", e.OldName, e.Name);
			sq.queueOperation(new OrphanScanOperation(ScanQueue.DEFAULT_DELAY));

			if (File.Exists(e.FullPath))
			{
				var dir = new FileInfo(e.FullPath).DirectoryName;
				sq.queueOperation(new FolderScanOperation(dir, ScanQueue.DEFAULT_DELAY));
			}

			if (Directory.Exists(e.FullPath))
			{
				sq.queueOperation(new FolderScanOperation(e.FullPath, ScanQueue.DEFAULT_DELAY));
			}
		}
	}
}
