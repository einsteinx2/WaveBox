using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.DataModel.FolderScanning
{
	enum OperationState
	{
		Queued,
		Waiting,
		Running,
		None
	}

	abstract class ScanOperation
	{
		private int DelaySeconds { get; set; }
		public bool ShouldRestart { get; set; }
		public bool ContinueWaiting { get; set; }

		private OperationState state = OperationState.None;
		public OperationState State
		{
			get
			{
				return state;
			}
		}

		public ScanOperation(int delay)
		{
			state = OperationState.Queued;
			DelaySeconds = delay;
		}

		public void Run()
		{
			state = OperationState.Waiting;

			do
			{
				ContinueWaiting = false;
				Thread.Sleep(DelaySeconds * 1000);
			}
			while (ContinueWaiting);

			state = OperationState.Running;
			Start();

			if (ShouldRestart == true)
				Run();

			state = OperationState.None;
		}

		public abstract string ScanType();
		public abstract void Start();

		public void ExtendWaitOrRestart()
		{
			if (state == OperationState.Waiting || state == OperationState.Queued)
			{
				ContinueWaiting = true;
				Console.WriteLine("Extending wait period.");
			}

			if (state == OperationState.Running)
			{
				ShouldRestart = true;
			}
		}
	}
}
