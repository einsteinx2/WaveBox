using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.FolderScanning;
using System.IO;
using WaveBox.OperationQueue;

namespace WaveBox.DataModel.Singletons
{
	class FileManager
	{
		private List<Folder> mediaFolders;
		private DelayedOperationQueue scanQueue;
		//private List<FileSystemWatcher> watcherList;

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

		public void Setup()
		{
			mediaFolders = Settings.MediaFolders;
			scanQueue = new DelayedOperationQueue();
			scanQueue.startScanQueue();
			scanQueue.queueOperation(new FolderScanning.OrphanScanOperation(0));

			foreach (var folder in mediaFolders)
			{
				scanQueue.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

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

			GC.Collect();
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
				scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
			}

			else if (Directory.Exists(e.FullPath))
			{
				scanQueue.queueOperation(new FolderScanOperation(e.FullPath, DelayedOperationQueue.DEFAULT_DELAY));
			}
		}

		private void OnDeleted(object source, FileSystemEventArgs e)
		{
			// if a file got deleted, we need to remove the orphan from the db
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));
		}

		private void OnRenamed(object source, RenamedEventArgs e)
		{
			// if a file is renamed, its db entry is probably orphaned.  remove the orphan and
			// add the renamed file as a new entry
			Console.WriteLine("[FILEMANAGER] {0} renamed to {1}", e.OldName, e.Name);
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));

			if (File.Exists(e.FullPath))
			{
				var dir = new FileInfo(e.FullPath).DirectoryName;
				scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
			}

			if (Directory.Exists(e.FullPath))
			{
				scanQueue.queueOperation(new FolderScanOperation(e.FullPath, DelayedOperationQueue.DEFAULT_DELAY));
			}
		}
	}
}
