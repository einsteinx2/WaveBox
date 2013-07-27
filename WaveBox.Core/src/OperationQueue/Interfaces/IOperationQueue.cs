using System;

namespace WaveBox.Core.OperationQueue
{
	public interface IOperationQueue
	{
		void startQueue();
		void stopQueue();
		void queueOperation(IDelayedOperation op);
	}
}

