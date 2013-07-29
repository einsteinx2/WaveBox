using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ninject;
using WaveBox.FolderScanning;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.Service;
using WaveBox.Static;
using System.Runtime.InteropServices;
using System.Threading;
using WaveBox.Core;

namespace WaveBox.Service.Services
{
	enum FSEventFlags
	{
		None = 0x00000000,
		MustScanSubDirs = 0x00000001,
		UserDropped = 0x00000002,
		KernelDropped = 0x00000004,
		EventIdsWrapped = 0x00000008,
		HistoryDone = 0x00000010,
		RootChanged = 0x00000020,
		FlagMount = 0x00000040,
		FlagUnmount = 0x00000080,
		// The below values are only relevant of file events are enabled
		FlagItemCreated = 0x00000100,
		FlagItemRemoved = 0x00000200,
		FlagItemInodeMetaMod = 0x00000400,
		FlagItemRenamed = 0x00000800,
		FlagItemModified = 0x00001000,
		FlagItemFinderInfoMod = 0x00002000,
		FlagItemChangeOwner = 0x00004000,
		FlagItemXattrMod = 0x00008000,
		FlagItemIsFile = 0x00010000,
		FlagItemIsDir = 0x00020000,
		FlagItemIsSymlink = 0x00040000
	}

	public class FileManagerService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "filemanager"; } set { } }

		public bool Required { get { return true; } set { } }

		public bool Running { get; set; }

		// Our list of media folders and the scanning queue which uses them
		private static IList<Folder> mediaFolders;
		private static DelayedOperationQueue scanQueue;

		private static Thread fsEventsThread = null;
		private static WatchCallback fsEventsCallback;

		// OS X FSEvents functions
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void WatchCallback(IntPtr changedPaths, int numberOfPaths, IntPtr eventFlags, bool fileEventsEnabled);

		[DllImport("libWaveBoxFSEvents")]
		public static extern void WatchPaths(IntPtr[] paths, int numberOfPaths, double latency, [MarshalAs(UnmanagedType.FunctionPtr)] WatchCallback callback);

		[DllImport("libWaveBoxFSEvents")]
		public static extern void StopWatchingPaths();

		public void FSEventsCallback(IntPtr changedPaths, int numberOfPaths, IntPtr eventFlags, bool fileEventsEnabled)
		{
			// Since it's a pointer to an array of pointers, first we need to convert it to an array of IntPtrs
			IntPtr[] stringPointerArray = new IntPtr[numberOfPaths];
			Marshal.Copy(changedPaths, stringPointerArray, 0, numberOfPaths);

			// Now create c# strings from the char arrays
			string [] pathStrings = new string[numberOfPaths];
			for (int i = 0; i < numberOfPaths; i++)
			{
				pathStrings[i] = Marshal.PtrToStringAnsi(stringPointerArray[i]);
			}

			// Covert the eventFlags pointer into an int array
			int[] eventFlagInts = new int[numberOfPaths];
			Marshal.Copy(eventFlags, eventFlagInts, 0, (int)numberOfPaths);

			// Process the changes
			for (int j = 0; j < numberOfPaths; j++)
			{
				string path = pathStrings[j];
				int eventFlag = eventFlagInts[j];
				if (fileEventsEnabled)
				{
					// We're on OS X 10.7 or above so we get file events. So we can handle things properly
					if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemCreated) == FSEventFlags.FlagItemCreated)
					{
						ItemCreated(path);
						logger.Info("FSEvents - item created at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemRemoved) == FSEventFlags.FlagItemRemoved)
					{
						ItemDeleted();
						logger.Info("FSEvents - item deleted at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemRenamed) == FSEventFlags.FlagItemRenamed)
					{
						ItemRenamed(path);
						logger.Info("FSEvents - item renamed at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemModified) == FSEventFlags.FlagItemModified)
					{
						// Do nothing for now
						logger.Info("FSEvents - item modified at path: " + path);
					}
					else
					{
						// For now, in any other cases, just do the safe thing and do an orphan scan as well
						ItemDeleted();
						ItemCreated(path);
						logger.Info("FSEvents - other event at path: " + path);
					}
				}
				else
				{
					// We're on OS X 10.6 or lower, we only get the changed folder name, so we always need to run an orphan scan
					ItemDeleted();
					ItemCreated(path);
				}
			}
		}

		/// <summary>
		/// Start() grabs the list of media folders from Settings, checks if they exist, and then begins to scan
		/// them for media fields
		/// </summary>
		public bool Start()
		{
			// Grab list of media folders, initialize the scan queue
			mediaFolders = Injection.Kernel.Get<IServerSettings>().MediaFolders;
			scanQueue = new DelayedOperationQueue();
			scanQueue.startQueue();
			scanQueue.queueOperation(new FolderScanning.OrphanScanOperation(0));

			if (ServerUtility.DetectOS() == ServerUtility.OS.MacOSX)
			{
				// The OS X implementation of FileSystemWatcher is based on the legacy BSD kevent API.
				// This has a frustrating limitation of requiring each folder and subfolder to be watched
				// individually. This quickly eats up all alloted open file handles when used with anything
				// larger than a trivial media collection, not to mention all the extra memory.
				//
				// So we've written a native dylib that uses the new OS X FSEvents API, which we can
				// communicate with here. That allows for only the top level folders to be watched.
				IntPtr[] paths = new IntPtr[mediaFolders.Count];
				int i = 0;
				foreach (Folder folder in mediaFolders)
				{
					// Launch the folder scan operation
					scanQueue.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

					paths[i] = Marshal.StringToHGlobalAnsi(folder.FolderPath);
					i++;
				}

				fsEventsCallback = new WatchCallback(FSEventsCallback);

				fsEventsThread = new Thread(() => { WatchPaths(paths, mediaFolders.Count, 5.0, fsEventsCallback); });
				fsEventsThread.Start();
			}
			else
			{
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

						// Confirm watcher addition
						if (logger.IsInfoEnabled) logger.Info("File system watcher added for: " + folder.FolderPath);
					}
					else
					{
						// Print an error if the folder doesn't exist
						if (logger.IsInfoEnabled) logger.Warn("Folder {0} does not exist, skipping... " + folder.FolderPath);
					}
				}
			}

			// Report if no media folders in configuration
			if (mediaFolders.Count == 0)
			{
				logger.Warn("No media folders defined, FileManager service cannot find any media");
			}

			// Collect garbage now to conserve resources
			GC.Collect();

			return true;
		}

		public bool Stop()
		{
			scanQueue.stopQueue();

			if (fsEventsThread != null)
			{
				StopWatchingPaths();
				fsEventsThread = null;
			}

			return true;
		}

		/// <summary>
		/// OnChanged() is currently a stub.
		/// </summary>
		private void OnChanged(object source, FileSystemEventArgs e)
		{
		}

		/// <summary>
		/// OnCreated() handles when an item is created in the watch folders, and forces a re-scan of the specific
		/// folder in which the file was created.
		/// </summary>
		private void OnCreated(object source, FileSystemEventArgs e)
		{
			ItemCreated(e.FullPath);
		}

		/// <summary>
		/// OnDeleted() handles when an object is deleted from a watch folder, starting an orphan scan on the database
		/// </summary>
		private void OnDeleted(object source, FileSystemEventArgs e)
		{
			ItemDeleted();
		}

		/// <summary>
		/// OnRenamed() handles when an object is renamed in a watch folder, purging the old object and adding the
		/// new one.
		/// </summary>
		private void OnRenamed(object source, RenamedEventArgs e)
		{
			if (logger.IsInfoEnabled) logger.Info(e.OldName + " renamed to " + e.Name);

			ItemRenamed(e.FullPath);
		}

		private void ItemCreated(string fullPath)
		{
			if (logger.IsInfoEnabled) logger.Info("File created: " + fullPath);

			// If a file is detected, start a scan of the folder it exists in
			if (File.Exists(fullPath))
			{
				if (logger.IsInfoEnabled) logger.Info("New file detected, starting scanning operation.");

				string dir = new FileInfo(fullPath).DirectoryName;
				scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
			}
			// If a directory is created, start a scan of the directory
			else if (Directory.Exists(fullPath))
			{
				if (logger.IsInfoEnabled) logger.Info("New directory detected, starting scanning operation.");
				scanQueue.queueOperation(new FolderScanOperation(fullPath, DelayedOperationQueue.DEFAULT_DELAY));
			}
			// Else, edge-case?  Might pick up something weird like a named pipe or socket with a valid media extension.
			else
			{
				if (logger.IsInfoEnabled) logger.Warn("Unknown object detected in filesystem at " + fullPath + ", ignoring...");
			}
		}

		private void ItemDeleted()
		{
			// if a file got deleted, we need to remove the orphan from the db
			scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));
		}

		private void ItemRenamed(string fullPath)
		{
			// if a file is renamed, its db entry is probably orphaned.  remove the orphan and
			// add the renamed file as a new entry
			// To easily accomplish the above, we just call OnDeleted() and OnCreated(), to reduce redundancy
			ItemDeleted();
			ItemCreated(fullPath);
		}
	}
}
