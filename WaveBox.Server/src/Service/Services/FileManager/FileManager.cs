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

namespace WaveBox.Service.Services.FileManager {
    public class FileManager : AbstractFileManager, IFileManager {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override bool Start() {
            // Grab list of media folders, initialize the scan queue
            IList<Folder> mediaFolders = Injection.Kernel.Get<IFolderRepository>().MediaFolders();
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
                    FileSystemWatcher watch = new FileSystemWatcher(folder.FolderPath);
                    watch.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watch.Changed += new FileSystemEventHandler(OnChanged);
                    watch.Created += new FileSystemEventHandler(OnCreated);
                    watch.Deleted += new FileSystemEventHandler(OnDeleted);
                    watch.Renamed += new RenamedEventHandler(OnRenamed);

                    watch.IncludeSubdirectories = true;
                    watch.EnableRaisingEvents = true;

                    // Confirm watcher addition
                    logger.IfInfo("File system watcher added for: " + folder.FolderPath);
                } else {
                    // Print an error if the folder doesn't exist
                    if (logger.IsInfoEnabled) { logger.Warn("Folder {0} does not exist, skipping... " + folder.FolderPath); }
                }
            }

            /*
            // Disabled until new web services in place - MDL, 11/11/13

            // Queue the musicbrainz scan after the folder scan
            this.scanQueue.queueOperation(new MusicBrainzScanOperation(0));

            // Queue the artist thumbnail downloader
            this.scanQueue.queueOperation(new ArtistThumbnailDownloadOperation(0));
            */

            // Report if no media folders in configuration
            if (mediaFolders.Count == 0) {
                logger.Warn("No media folders defined, FileManager service cannot find any media");
            }

            // Collect garbage now to conserve resources
            GC.Collect();

            return true;
        }

        public override bool Stop() {
            this.scanQueue.stopQueue();

            return true;
        }
    }
}
