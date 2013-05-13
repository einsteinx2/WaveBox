﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Singletons;
using WaveBox.Model;
using WaveBox.FolderScanning;
using System.IO;
using WaveBox.OperationQueue;

namespace WaveBox.Singletons
{
	class FileManager
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Our list of media folders and the scanning queue which uses them
		private List<Folder> mediaFolders;
		private DelayedOperationQueue scanQueue;
		//private List<FileSystemWatcher> watcherList;

		// FileManager singleton
		private FileManager() { }
		private static readonly FileManager instance = new FileManager();
		public static FileManager Instance { get { return instance; } }

		/// <summary>
		/// Setup() grabs the list of media folders from Settings, checks if they exist, and then begins to scan
		/// them for media fields
		/// </summary>
		public void Setup()
		{
			// Grab list of media folders, initialize the scan queue
			mediaFolders = Settings.MediaFolders;
			scanQueue = new DelayedOperationQueue();
			scanQueue.startQueue();
			scanQueue.queueOperation(new FolderScanning.OrphanScanOperation(0));

			// Iterate the list of folders
			foreach (Folder folder in mediaFolders)
			{
				// Sanity check, for my sanity.  Why start a scanning operation if the folder doesn't exist?
				if(Directory.Exists(folder.FolderPath))
				{
					// Launch the folder scan operation
					scanQueue.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

					// Create filesystem watchers, begin watching the files for changes
					FileSystemWatcher watch = new FileSystemWatcher(folder.FolderPath);
					watch.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
					watch.Changed += new FileSystemEventHandler(OnChanged);
					watch.Created += new FileSystemEventHandler(OnCreated);
					watch.Deleted += new FileSystemEventHandler(OnDeleted);
					watch.Renamed += new RenamedEventHandler(OnRenamed);

					watch.IncludeSubdirectories = true;
					watch.EnableRaisingEvents = true;

					if (WaveBoxService.DetectOS() == WaveBoxService.OS.MacOSX)
					{
						// On OS X, there is a bug that requires us to explicitly set
						// watchers for all subdirectories. The IncludeSubdirectories
						// property is ignored
						Stack<string> dirs = new Stack<string>(20);
						dirs.Push(folder.FolderPath);

						while (dirs.Count > 0)
						{
							string currentDir = dirs.Pop();
							string[] subDirs;
							try
							{
								subDirs = System.IO.Directory.GetDirectories(currentDir);
							}
							catch (UnauthorizedAccessException e)
							{                    
								logger.Error("Access denied: " + e);
								continue;
							}
							catch (System.IO.DirectoryNotFoundException e)
							{
								logger.Error(e);
								continue;
							}

							foreach (string subDirectory in subDirs)
							{
								watch = new FileSystemWatcher(subDirectory);
								watch.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
								watch.Changed += new FileSystemEventHandler(OnChanged);
								watch.Created += new FileSystemEventHandler(OnCreated);
								watch.Deleted += new FileSystemEventHandler(OnDeleted);
								watch.Renamed += new RenamedEventHandler(OnRenamed);
								watch.EnableRaisingEvents = true;

								if (logger.IsInfoEnabled) logger.Info("File system watcher added for: " + subDirectory);

								dirs.Push(subDirectory);
							}
						}
					}

					// Confirm watcher addition
					if (logger.IsInfoEnabled) logger.Info("File system watcher added for: " + folder.FolderPath);
					//watcherList.Add(watch);
				}
				else
				{
					// Print an error if the folder doesn't exist
					if (logger.IsInfoEnabled) logger.Info("warning: folder {0} does not exist, skipping... " + folder.FolderPath);
				}
			}

			// Collect garbage now to conserve resources
			GC.Collect();
		}

		public void Stop()
		{
			scanQueue.stopQueue();
		}

		/// <summary>
		/// OnChanged() is currently a stub.
		/// </summary>
		private void OnChanged(object source, FileSystemEventArgs e)
		{
			// Specify what is done when a file is changed, created, or deleted.
			//if (logger.IsInfoEnabled) logger.Info("File: " + e.FullPath + " " + e.ChangeType);
		}

		/// <summary>
		/// OnCreated() handles when an item is created in the watch folders, and forces a re-scan of the specific
		/// folder in which the file was created.
		/// </summary>
		private void OnCreated(object source, FileSystemEventArgs e)
		{
			if (logger.IsInfoEnabled) logger.Info("File created: " + e.FullPath);

			// If a file is detected, start a scan of the folder it exists in
			if (File.Exists(e.FullPath))
			{
				if (logger.IsInfoEnabled) logger.Info("New file detected, starting scanning operation.");

				string dir = new FileInfo(e.FullPath).DirectoryName;
				scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
			}
			// If a directory is created, start a scan of the directory
			else if (Directory.Exists(e.FullPath))
			{
				if (logger.IsInfoEnabled) logger.Info("New directory detected, starting scanning operation.");
				scanQueue.queueOperation(new FolderScanOperation(e.FullPath, DelayedOperationQueue.DEFAULT_DELAY));
			}
			// Else, edge-case?  Might pick up something weird like a named pipe or socket with a valid media extension.
			else
			{
				if (logger.IsInfoEnabled) logger.Info("warning: unknown object detected in filesystem at " + e.FullPath + ", ignoring...");
			}
		}

		/// <summary>
		/// OnDeleted() handles when an object is deleted from a watch folder, starting an orphan scan on the database
		/// </summary>
		private void OnDeleted(object source, FileSystemEventArgs e)
		{
			// if a file got deleted, we need to remove the orphan from the db
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));
		}

		/// <summary>
		/// OnRenamed() handles when an object is renamed in a watch folder, purging the old object and adding the
		/// new one.
		/// </summary>
		private void OnRenamed(object source, RenamedEventArgs e)
		{
			// if a file is renamed, its db entry is probably orphaned.  remove the orphan and
			// add the renamed file as a new entry
			if (logger.IsInfoEnabled) logger.Info(e.OldName + " renamed to " + e.Name);

			// To easily accomplish the above, we just call OnDeleted() and OnCreated(), to reduce redundancy
			OnDeleted(source, e);
			OnCreated(source, e);

			/*
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));

			if (File.Exists(e.FullPath))
			{
				string dir = new FileInfo(e.FullPath).DirectoryName;
				scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
			}

			if (Directory.Exists(e.FullPath))
			{
				scanQueue.queueOperation(new FolderScanOperation(e.FullPath, DelayedOperationQueue.DEFAULT_DELAY));
			}
			*/
		}
	}
}
