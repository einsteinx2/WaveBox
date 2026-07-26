using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.OperationQueue {
    public class DelayedOperationQueue : IOperationQueue {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        public const int DEFAULT_DELAY = 10;
        public const int DEFAULT_PRECISION = 250;

        private IDelayedOperation currentOperation;
        public IDelayedOperation CurrentOperation { get { return currentOperation; } }

        private Thread queueThread;
        private readonly CancellationTokenSource queueCts = new CancellationTokenSource();
        private Queue<IDelayedOperation> operationQueue = new Queue<IDelayedOperation>();

        public void startQueue() {
            CancellationToken token = queueCts.Token;
            queueThread = new Thread(delegate() {
                while (!token.IsCancellationRequested) {
                    lock (operationQueue) {
                        if (operationQueue.Count > 0 && operationQueue.Peek() != null && operationQueue.Peek().IsReady) {
                            try {
                                currentOperation = operationQueue.Dequeue();
                            } catch {
                                currentOperation = null;
                            }

                            if (currentOperation != null) {
                                currentOperation.Run();
                                logger.IfInfo(currentOperation.ToString() + " fired");
                            }
                        }
                    }

                    // Sleep to prevent a tight loop, waking immediately on cancellation
                    if (token.WaitHandle.WaitOne(DEFAULT_PRECISION)) {
                        break;
                    }
                }
            });
            queueThread.IsBackground = true;
            queueThread.Start();
        }

        public void stopQueue() {
            // Cooperative cancellation (Thread.Abort throws PlatformNotSupportedException on modern .NET)
            queueCts.Cancel();
            if (queueThread != null) {
                queueThread.Join();
            }
        }

        public void queueOperation(IDelayedOperation op) {
            lock (operationQueue) {
                if (operationQueue.Contains(op)) {
                    // This operation is already queued, see if it's running
                    if (op.Equals(CurrentOperation)) {
                        // It's running, restart it
                        op.Restart();
                    } else {
                        // It's still queued, if it's first up, reset it's wait
                        IDelayedOperation firstOp = operationQueue.Peek();
                        if (firstOp.Equals(op)) {
                            op.ResetWait();
                        }
                    }
                    logger.IfInfo("ExtendWaitOrRestart " + op.OperationType + "!");
                } else {
                    operationQueue.Enqueue(op);
                    logger.IfInfo("Queuing new " + op.OperationType + "!");
                }
            }
        }
    }
}
