using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.FolderScanning;
using WaveBox.Model;
using WaveBox.OperationQueue;
using WaveBox.Static;

namespace WaveBox.Static
{
	public static class FileManager
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Our list of media folders and the scanning queue which uses them
		private static List<Folder> mediaFolders;
		private static DelayedOperationQueue scanQueue;

		/// <summary>
		/// Setup() grabs the list of media folders from Settings, checks if they exist, and then begins to scan
		/// them for media fields
		/// </summary>
		public static void Setup()
		{
			// Grab list of media folders, initialize the scan queue
			mediaFolders = Injection.Kernel.Get<IServerSettings>().MediaFolders;
			scanQueue = new DelayedOperationQueue();
			scanQueue.startQueue();
			scanQueue.queueOperation(new FolderScanning.OrphanScanOperation(0));

			// Iterate the list of folders
			foreach (Folder folder in mediaFolders)
			{
				// Sanity check, for my sanity.  Why start a scanning operation if the folder doesn't exist?
				if (Directory.Exists(folder.FolderPath))
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

					if (ServerUtility.DetectOS() == ServerUtility.OS.MacOSX)
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

		public static void Stop()
		{
			scanQueue.stopQueue();
		}

		/// <summary>
		/// OnChanged() is currently a stub.
		/// </summary>
		private static void OnChanged(object source, FileSystemEventArgs e)
		{
		}

		/// <summary>
		/// OnCreated() handles when an item is created in the watch folders, and forces a re-scan of the specific
		/// folder in which the file was created.
		/// </summary>
		private static void OnCreated(object source, FileSystemEventArgs e)
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
				if (logger.IsInfoEnabled) logger.Warn("Unknown object detected in filesystem at " + e.FullPath + ", ignoring...");
			}
		}

		/// <summary>
		/// OnDeleted() handles when an object is deleted from a watch folder, starting an orphan scan on the database
		/// </summary>
		private static void OnDeleted(object source, FileSystemEventArgs e)
		{
			// if a file got deleted, we need to remove the orphan from the db
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));
		}

		/// <summary>
		/// OnRenamed() handles when an object is renamed in a watch folder, purging the old object and adding the
		/// new one.
		/// </summary>
		private static void OnRenamed(object source, RenamedEventArgs e)
		{
			// if a file is renamed, its db entry is probably orphaned.  remove the orphan and
			// add the renamed file as a new entry
			if (logger.IsInfoEnabled) logger.Info(e.OldName + " renamed to " + e.Name);

			// To easily accomplish the above, we just call OnDeleted() and OnCreated(), to reduce redundancy
			OnDeleted(source, e);
			OnCreated(source, e);
		}
	}
}
