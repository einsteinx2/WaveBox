using System;

namespace WaveBox.Core.OperationQueue
{
	public enum DelayedOperationState
	{
		Queued,
		Running,
		Completed,
		Canceled,
		None
	}

	public interface IDelayedOperation
	{
		DelayedOperationState State { get; }
		DateTime RunDateTime { get; set; }
		bool IsReady { get; }
		string OperationType { get; }

		void Run();
		void Cancel();
		void ResetWait();
		void Restart();
	}
}

