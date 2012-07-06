using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;

namespace MediaFerry.DataModel.Singletons
{
	class FileManager
	{
		List<Folder> mf;

		public FileManager()
		{
			mf = Settings.MediaFolders;
			var orphan = new FolderScanning.OrphanScanOperation(0);
			orphan.start();

			foreach (var folder in mf)
			{
				var scanner = new FolderScanning.FolderScanOperation(folder.FolderPath, 0);
				scanner.start();
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
	}
}
