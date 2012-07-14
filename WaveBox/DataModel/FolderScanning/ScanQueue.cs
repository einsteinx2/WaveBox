using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.DataModel.FolderScanning
{
	class ScanQueue
	{
		public const int DEFAULT_DELAY = 10;
		public string CurrentScanningFolder { get; set; }
		public string CurrentScanningFile { get; set; }

		private Thread scanQueueThread;
		public Thread ScanQueueThread
		{
			get
			{
				return scanQueueThread;
			}
		}

		private ScanOperation currentOperation;
		public ScanOperation CurrentOperation
		{
			get
			{
				return currentOperation;
			}
		}

		public ScanQueue()
		{
			sw = new Stopwatch();
		}

		private bool scanQueueShouldLoop = true;
		private Object scanQueueSyncObject = new Object();
		private Queue<ScanOperation> scanQueue = new Queue<ScanOperation>();
		Stopwatch sw;

		public void startScanQueue()
		{
			scanQueueThread = new Thread(delegate() 
			{
				while (scanQueueShouldLoop)
				{
					lock (scanQueueSyncObject)
					{
						try
						{
							currentOperation = scanQueue.Dequeue();
						}
						catch
						{
							//Console.WriteLine("[SCANQUEUE] Queue is empty: ", e.Message);
							currentOperation = null;
						}
					}

					if (currentOperation != null)
					{
						sw.Start();
						currentOperation.Run();
						sw.Stop();
						Console.WriteLine("[SCANQUEUE] Scan took {0} seconds", sw.ElapsedMilliseconds / 1000);
						sw.Reset();
					}

					// sleep for a second
					Thread.Sleep(new TimeSpan(0, 0, 1));
				}

			});
			scanQueueThread.Start();
		}

		public void stopScanQueue()
		{
			scanQueueShouldLoop = false;
			scanQueueThread.Abort();
			scanQueue.Clear();
		}

		public void queueOperation(ScanOperation op)
		{
			lock (scanQueueSyncObject)
			{
				bool shouldBeAddedToQueue = true;

				if (currentOperation != null && (op.ScanType().Contains(currentOperation.ScanType()) || currentOperation.ScanType().Contains(op.ScanType())))
				{
					currentOperation.ExtendWaitOrRestart();
					shouldBeAddedToQueue = false;
				}

				if (shouldBeAddedToQueue == true)
				{
					foreach (ScanOperation o in scanQueue)
					{
						string opscantype = op.ScanType();
						string oscantype = o.ScanType();

						if (opscantype.Contains(oscantype) || oscantype.Contains(opscantype))
						{
							// I don't think I really need to do anything in this case.  If it's not running and it's in the queue,
							// I should just leave it in the queue and let it do its thing when the time comes.  Then, at that time,
							// I can decide stuff.

							shouldBeAddedToQueue = false;
							break;
						}
					}
				}

				if (shouldBeAddedToQueue)
				{
					scanQueue.Enqueue(op);
					Console.WriteLine("[SCANQUEUE] New {0}!", op.GetType());
				}
			}
		}
	}
}
