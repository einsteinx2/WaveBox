using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.FolderScanning;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Service.Services.FileManager
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

	public class MacOSXFileManager : AbstractFileManager, IFileManager
	{
		// The OS X implementation of FileSystemWatcher is based on the legacy BSD kevent API.
		// This has a frustrating limitation of requiring each folder and subfolder to be watched
		// individually. This quickly eats up all alloted open file handles when used with anything
		// larger than a trivial media collection, not to mention all the extra memory.
		//
		// So we've written a native dylib that uses the new OS X FSEvents API, which we can
		// communicate with here. That allows for only the top level folders to be watched.

		private Thread fsEventsThread = null;
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
						logger.IfInfo("FSEvents - item created at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemRemoved) == FSEventFlags.FlagItemRemoved)
					{
						ItemDeleted();
						logger.IfInfo("FSEvents - item deleted at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemRenamed) == FSEventFlags.FlagItemRenamed)
					{
						ItemRenamed(path);
						logger.IfInfo("FSEvents - item renamed at path: " + path);
					}
					else if (((FSEventFlags)eventFlag & FSEventFlags.FlagItemModified) == FSEventFlags.FlagItemModified)
					{
						// Do nothing for now
						logger.IfInfo("FSEvents - item modified at path: " + path);
					}
					else
					{
						// For now, in any other cases, just do the safe thing and do an orphan scan as well
						ItemDeleted();
						ItemCreated(path);
						logger.IfInfo("FSEvents - other event at path: " + path);
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

		public override bool Start()
		{
			// Grab list of media folders, initialize the scan queue
			IList<Folder> mediaFolders = Injection.Kernel.Get<IFolderRepository>().MediaFolders();
			this.scanQueue = new DelayedOperationQueue();
			this.scanQueue.startQueue();
			this.scanQueue.queueOperation(new OrphanScanOperation(0));

			IntPtr[] paths = new IntPtr[mediaFolders.Count];
			int i = 0;
			foreach (Folder folder in mediaFolders)
			{
				// Launch the folder scan operation
				this.scanQueue.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

				paths[i] = Marshal.StringToHGlobalAnsi(folder.FolderPath);
				i++;
			}

			fsEventsCallback = new WatchCallback(FSEventsCallback);

			this.fsEventsThread = new Thread(() => { WatchPaths(paths, mediaFolders.Count, 5.0, fsEventsCallback); });
			this.fsEventsThread.Start();

			/*
			// Disabled until new web services in place - MDL, 11/11/13

			// Queue the musicbrainz scan after the folder scan
			this.scanQueue.queueOperation(new MusicBrainzScanOperation(0));

			// Queue the artist thumbnail downloader
			this.scanQueue.queueOperation(new ArtistThumbnailDownloadOperation(0));
			*/

			// Report if no media folders in configuration
			if (mediaFolders.Count == 0)
			{
				logger.Warn("No media folders defined, FileManager service cannot find any media");
			}

			// Collect garbage now to conserve resources
			GC.Collect();

			return true;
		}

		public override bool Stop()
		{
			this.scanQueue.stopQueue();

			if (this.fsEventsThread != null)
			{
				StopWatchingPaths();
				this.fsEventsThread = null;
			}

			return true;
		}
	}
}
