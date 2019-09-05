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
using WaveBox.Service.Services.FileManager;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Service.Services {
    public class FileManagerService : IService {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "filemanager"; } set { } }

        public bool Required { get { return true; } set { } }

        public bool Running { get; set; }

        /// <summary>
        /// Start() grabs the list of media folders from Settings, checks if they exist, and then begins to scan
        /// them for media fields
        /// </summary>
        public bool Start() {
            Injection.Kernel.Get<IFileManager>().Start();

            this.Running = true;
            return true;
        }

        public bool Stop() {
            Injection.Kernel.Get<IFileManager>().Stop();

            this.Running = false;
            return true;
        }
    }
}
