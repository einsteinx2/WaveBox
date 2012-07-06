using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using MediaFerry.DataModel.FolderScanning;

namespace MediaFerry.DataModel.Singletons
{
	class FileManager
	{
		List<Folder> mf;
		private ScanQueue sq;

		public FileManager()
		{
			mf = Settings.MediaFolders;
			sq = new ScanQueue();
			sq.startScanQueue();
			sq.queueOperation(new FolderScanning.OrphanScanOperation(0));

			foreach (var folder in mf)
			{
				sq.queueOperation(new FolderScanning.FolderScanOperation(folder.FolderPath, 0));
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
