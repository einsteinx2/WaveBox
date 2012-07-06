using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MediaFerry.DataModel.FolderScanning
{
	abstract class ScanOperation
	{
		private int _secondsDelay;
		private bool _isRestart;
		private Timer _t;
		TimerCallback _tcb;

		public bool IsRestart
		{
			get
			{
				return _isRestart;
			}
			set
			{
				_isRestart = value;
			}
		}

		public int Delay
		{
			get
			{
				return _secondsDelay;
			}
		}


		public ScanOperation(int secondsDelay)
		{
			_secondsDelay = secondsDelay;
			TimerCallback _tcb = timerComplete;
			resetDelay();
		}

		public ScanOperation()
		{
		}

		public void resetDelay()
		{
			_t.Dispose();
			_t = new Timer(timerComplete, null, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, _secondsDelay));
		}

		private void timerComplete(Object source)
		{
		}

		public void run()
		{
			do
			{
				IsRestart = false;
				start();
			} 
			while (IsRestart);
		}

		public abstract void start();
	}
}
