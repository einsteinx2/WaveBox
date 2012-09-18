using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.OperationQueue
{
	public class DelayedOperationQueue
	{
		public const int DEFAULT_DELAY = 10;
		public const int DEFAULT_PRECISION = 250;

		private IDelayedOperation currentOperation;
		public IDelayedOperation CurrentOperation { get { return currentOperation; } }

		private Thread queueThread;
		private bool queueShouldLoop = true;
		private Queue<IDelayedOperation> operationQueue = new Queue<IDelayedOperation>();
		Stopwatch sw = new Stopwatch();

		public void startScanQueue()
		{
			queueThread = new Thread(delegate() 
			{
				while (queueShouldLoop)
				{
					lock (operationQueue)
					{
						if (operationQueue.Count > 0 && operationQueue.Peek() != null && operationQueue.Peek().IsReady)
						{
							try
							{
								currentOperation = operationQueue.Dequeue();
							}
							catch
							{
								//Console.WriteLine("[SCANQUEUE] Queue is empty: ", e.Message);
								currentOperation = null;
							}

							if (currentOperation != null)
							{
								currentOperation.Run();
                                Console.WriteLine("[DELAYEDOPQUEUE] {0} fired", currentOperation.ToString());
							}
						}
					}

					// Sleep to prevent a tight loop
					Thread.Sleep(DEFAULT_PRECISION);
				}
			});
			queueThread.Start();
		}

		public void stopScanQueue()
		{
			queueShouldLoop = false; // Break the loop
			queueThread.Abort(); // Abort the thread
			queueThread.Join(); // Wait for the thread to die
		}

		public void queueOperation(IDelayedOperation op)
		{
			lock (operationQueue)
			{
				if (operationQueue.Contains(op))
				{
					// This operation is already queued, see if it's running
					if (op.Equals(CurrentOperation))
					{
						// It's running, restart it
						op.Restart();
					}
					else
					{
						// It's still queued, if it's first up, reset it's wait
						IDelayedOperation firstOp = operationQueue.Peek();
						if (firstOp.Equals(op))
						{
							op.ResetWait();
						}
					}
					Console.WriteLine("[SCANQUEUE] ExtendWaitOrRestart {0}!", op.OperationType);
				}
				else
				{
					operationQueue.Enqueue(op);
					Console.WriteLine("[SCANQUEUE] New {0}!", op.OperationType);
				}
			}
		}
	}
}
