using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.FolderScanning;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Service.Services.FileManager {
    // Single cross-platform file watcher.  Modern .NET's FileSystemWatcher is FSEvents-backed on
    // macOS, inotify-backed on Linux, and native on Windows, so the old MacOSXFileManager with its
    // custom FSEvents dylib is no longer needed.
    public class FileManager : AbstractFileManager, IFileManager {
        private static new readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        // Keep strong references so the watchers can't be garbage collected
        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        public override bool Start() {
            // Grab list of media folders, initialize the scan queue
            IList<Folder> mediaFolders = Injection.Get<IFolderRepository>().MediaFolders();
            this.scanQueue = new DelayedOperationQueue();
            this.scanQueue.startQueue();
            this.scanQueue.queueOperation(new OrphanScanOperation(0));

            // Iterate the list of folders
            foreach (Folder folder in mediaFolders) {
                // Sanity check, for my sanity.  Why start a scanning operation if the folder doesn't exist?
                if (Directory.Exists(folder.FolderPath)) {
                    // Launch the folder scan operation
                    this.scanQueue.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));

                    // Create filesystem watchers, begin watching the files for changes
                    this.WatchFolder(folder.FolderPath);
                } else {
                    // Print an error if the folder doesn't exist
                    logger.Warn("Folder does not exist, skipping... " + folder.FolderPath);
                }
            }

            // Report if no media folders in configuration
            if (mediaFolders.Count == 0) {
                logger.Warn("No media folders defined, FileManager service cannot find any media");
            }

            return true;
        }

        public override bool Stop() {
            foreach (FileSystemWatcher watch in this.watchers) {
                watch.EnableRaisingEvents = false;
                watch.Dispose();
            }
            this.watchers.Clear();

            this.scanQueue.stopQueue();

            return true;
        }

        private void WatchFolder(string folderPath) {
            FileSystemWatcher watch = new FileSystemWatcher(folderPath);
            watch.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watch.Changed += new FileSystemEventHandler(OnChanged);
            watch.Created += new FileSystemEventHandler(OnCreated);
            watch.Deleted += new FileSystemEventHandler(OnDeleted);
            watch.Renamed += new RenamedEventHandler(OnRenamedWithOldPath);
            watch.Error += (sender, e) => this.OnWatcherError((FileSystemWatcher)sender, folderPath, e);

            // Largest supported buffer, so event bursts (e.g. copying an album in) don't overflow
            watch.InternalBufferSize = 64 * 1024;
            watch.IncludeSubdirectories = true;
            watch.EnableRaisingEvents = true;

            this.watchers.Add(watch);

            // Confirm watcher addition
            logger.IfInfo("File system watcher added for: " + folderPath);
        }

        // On top of the base rename handling (orphan scan + scan of the new location), also rescan
        // the old parent directory so moves within a media folder reconcile both ends
        private void OnRenamedWithOldPath(object source, RenamedEventArgs e) {
            OnRenamed(source, e);

            try {
                string oldDir = Path.GetDirectoryName(e.OldFullPath);
                if (oldDir != null && Directory.Exists(oldDir)) {
                    this.scanQueue.queueOperation(new FolderScanOperation(oldDir, DelayedOperationQueue.DEFAULT_DELAY));
                }
            } catch (Exception ex) {
                logger.Error("Error queueing scan of rename source directory", ex);
            }
        }

        // A watcher error usually means its buffer overflowed and events were lost; recreate the
        // watcher and queue a full rescan of that root so nothing stays missed
        private void OnWatcherError(FileSystemWatcher watch, string folderPath, ErrorEventArgs e) {
            logger.Warn("File system watcher error for " + folderPath + ", recreating watcher and rescanning", e.GetException());

            try {
                watch.EnableRaisingEvents = false;
                watch.Dispose();
                this.watchers.Remove(watch);
            } catch (Exception ex) {
                logger.Error("Error disposing failed watcher", ex);
            }

            if (Directory.Exists(folderPath)) {
                this.WatchFolder(folderPath);
                this.scanQueue.queueOperation(new FolderScanOperation(folderPath, DelayedOperationQueue.DEFAULT_DELAY));
            }
        }
    }
}
