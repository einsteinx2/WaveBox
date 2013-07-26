using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace WaveBox.Core.OperationQueue
{
	public class DelayedOperationQueue
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const int DEFAULT_DELAY = 10;
		public const int DEFAULT_PRECISION = 250;

		private IDelayedOperation currentOperation;
		public IDelayedOperation CurrentOperation { get { return currentOperation; } }

		private Thread queueThread;
		private bool queueShouldLoop = true;
		private Queue<IDelayedOperation> operationQueue = new Queue<IDelayedOperation>();

		public void startQueue()
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
								currentOperation = null;
							}

							if (currentOperation != null)
							{
								currentOperation.Run();
								if (logger.IsInfoEnabled) logger.Info(currentOperation.ToString() + " fired");
							}
						}
					}

					// Sleep to prevent a tight loop
					Thread.Sleep(DEFAULT_PRECISION);
				}
			});
			queueThread.IsBackground = true;
			queueThread.Start();
		}

		public void stopQueue()
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
					if (logger.IsInfoEnabled) logger.Info("ExtendWaitOrRestart " + op.OperationType + "!");
				}
				else
				{
					operationQueue.Enqueue(op);
					if (logger.IsInfoEnabled) logger.Info("Queuing new " + op.OperationType + "!");
				}
			}
		}
	}
}
