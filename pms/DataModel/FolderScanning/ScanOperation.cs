using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaFerry.DataModel.FolderScanning
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
		private int _delaySeconds;

		private bool _shouldRestart;
		public bool ShouldRestart
		{
			get
			{
				return _shouldRestart;
			}
			set
			{
				_shouldRestart = value;
			}
		}

		private bool _continueWaiting;
		public bool ContinueWaiting
		{
			get
			{
				return _continueWaiting;
			}
			set
			{
				_continueWaiting = value;
			}
		}

		private OperationState _state = OperationState.None;
		public OperationState State
		{
			get
			{
				return _state;
			}
		}

		public ScanOperation(int delay)
		{
			_state = OperationState.Queued;
			_delaySeconds = delay;
		}

		public void Run()
		{
			_state = OperationState.Waiting;

			do
			{
				ContinueWaiting = false;
				Thread.Sleep(_delaySeconds * 1000);
			}
			while (ContinueWaiting);

			_state = OperationState.Running;
			Start();

			if (ShouldRestart == true)
				Run();

			_state = OperationState.None;
		}

		public abstract string ScanType();
		public abstract void Start();

		public void ExtendWaitOrRestart()
		{
			if (_state == OperationState.Waiting || _state == OperationState.Queued)
			{
				ContinueWaiting = true;
				Console.WriteLine("Extending wait period.");
			}

			if (_state == OperationState.Running)
			{
				ShouldRestart = true;
			}
		}
	}
}
