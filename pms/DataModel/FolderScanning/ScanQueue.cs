using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

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

		public ScanQueue()
		{
			sw = new Stopwatch();
		}

		private bool _scanQueueShouldLoop = true;
		private Thread _scanQueueThread;
		private Object _scanQueueSyncObject = new Object();
		private Queue<ScanOperation> _scanQueue = new Queue<ScanOperation>();
		private ScanOperation _currentOperation;
		Stopwatch sw;

		public void startScanQueue()
		{
			_scanQueueThread = new Thread(delegate() 
				{
					while (_scanQueueShouldLoop)
					{
						lock (_scanQueueSyncObject)
						{
							try
							{
								_currentOperation = _scanQueue.Dequeue();
							}
							catch
							{
								//Console.WriteLine("[SCANQUEUE] Queue is empty: ", e.Message);
								_currentOperation = null;
							}
						}

						if (_currentOperation != null)
						{
							sw.Start();
							_currentOperation.Run();
							sw.Stop();
							Console.WriteLine("[SCANQUEUE] Scan took {0} seconds", sw.ElapsedMilliseconds / 1000);
							sw.Reset();
						}

						// sleep for a second
						Thread.Sleep(new TimeSpan(0, 0, 1));
					}

				});
			_scanQueueThread.Start();
		}

		public void stopScanQueue()
		{
			_scanQueueShouldLoop = false;
			_scanQueueThread.Abort();
			_scanQueue.Clear();
		}

		public void queueOperation(ScanOperation op)
		{
			lock (_scanQueueSyncObject)
			{
				// if the operation at the head of the queue is the same as the currently running operation,
				// then the file changed while we were scanning.  Restart the scan.
				if (op.Equals(_currentOperation))
				{
					op.Restart();
				}

				else if (_scanQueue.Contains(op))
				{
					// I don't think I really need to do anything in this case.  If it's not running and it's in the queue,
					// I should just leave it in the queue and let it do its thing when the time comes.  Then, at that time,
					// I can decide stuff.
				}

				else
				{
					_scanQueue.Enqueue(op);
					Console.WriteLine("[SCANQUEUE] New {0}!", op.GetType());
				}
			}
		}
	}
}
