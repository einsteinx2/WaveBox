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
    public abstract class AbstractFileManager : IFileManager {
        protected static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Our list of media folders and the scanning queue which uses them
        protected DelayedOperationQueue scanQueue;

        abstract public bool Start();
        abstract public bool Stop();

        /// <summary>
        /// OnChanged() is currently a stub.
        /// </summary>
        public void OnChanged(object source, FileSystemEventArgs e) {
        }

        /// <summary>
        /// OnCreated() handles when an item is created in the watch folders, and forces a re-scan of the specific
        /// folder in which the file was created.
        /// </summary>
        public void OnCreated(object source, FileSystemEventArgs e) {
            ItemCreated(e.FullPath);
        }

        /// <summary>
        /// OnDeleted() handles when an object is deleted from a watch folder, starting an orphan scan on the database
        /// </summary>
        public void OnDeleted(object source, FileSystemEventArgs e) {
            ItemDeleted();
        }

        /// <summary>
        /// OnRenamed() handles when an object is renamed in a watch folder, purging the old object and adding the
        /// new one.
        /// </summary>
        public void OnRenamed(object source, RenamedEventArgs e) {
            logger.IfInfo(e.OldName + " renamed to " + e.Name);

            ItemRenamed(e.FullPath);
        }

        public void ItemCreated(string fullPath) {
            logger.IfInfo("File created: " + fullPath);

            // If a file is detected, start a scan of the folder it exists in
            if (File.Exists(fullPath)) {
                logger.IfInfo("New file detected, starting scanning operation.");

                string dir = new FileInfo(fullPath).DirectoryName;
                this.scanQueue.queueOperation(new FolderScanOperation(dir, DelayedOperationQueue.DEFAULT_DELAY));
            }
            // If a directory is created, start a scan of the directory
            else if (Directory.Exists(fullPath)) {
                logger.IfInfo("New directory detected, starting scanning operation.");
                this.scanQueue.queueOperation(new FolderScanOperation(fullPath, DelayedOperationQueue.DEFAULT_DELAY));
            }
            // Else, edge-case?  Might pick up something weird like a named pipe or socket with a valid media extension.
            else {
                if (logger.IsInfoEnabled) { logger.Warn("Unknown object detected in filesystem at " + fullPath + ", ignoring..."); }
            }
        }

        public void ItemDeleted() {
            // if a file got deleted, we need to remove the orphan from the db
            this.scanQueue.queueOperation(new OrphanScanOperation(DelayedOperationQueue.DEFAULT_DELAY));
        }

        public void ItemRenamed(string fullPath) {
            // if a file is renamed, its db entry is probably orphaned.  remove the orphan and
            // add the renamed file as a new entry
            // To easily accomplish the above, we just call OnDeleted() and OnCreated(), to reduce redundancy
            ItemDeleted();
            ItemCreated(fullPath);
        }
    }
}
