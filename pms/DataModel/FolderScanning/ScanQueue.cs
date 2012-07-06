using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MediaFerry.DataModel.FolderScanning
{
	class ScanQueue
	{
		private string _currentScanningFolder;
		public string CurrentScanningFolder
		{
			get
			{
				return _currentScanningFolder;
			}

			set
			{
				_currentScanningFolder = value;
			}
		}

		private string _currentScanningFile;
		public string CurrentScanningFile
		{
			get
			{
				return _currentScanningFile;
			}

			set
			{
				_currentScanningFile = value;
			}
		}

		private bool _scanQueueShouldLoop = true;
		private Thread _scanQueueThread;
		private Object _scanQueueSyncObject = new Object();
		private Queue<ScanOperation> _scanQueue = new Queue<ScanOperation>();
		private ScanOperation _currentOperation;

		public void startScanQueue()
		{
			_scanQueueThread = new Thread(delegate
		}
	}
}
