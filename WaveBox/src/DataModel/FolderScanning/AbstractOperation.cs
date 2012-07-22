using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.OperationQueue
{
	public abstract class AbstractOperation : IDelayedOperation
	{
		public bool IsReady { get { return DateTime.UtcNow.CompareTo(RunDateTime) >= 0; } }

		public abstract string OperationType { get; }

		private DelayedOperationState state = DelayedOperationState.None;
		public DelayedOperationState State { get { return state; } }

		public DateTime RunDateTime { get; set; }

		private int delayMillis;

		protected bool isRestart = false;

		public AbstractOperation(int delayMilliSeconds)
		{
			state = DelayedOperationState.Queued;

			delayMillis = delayMilliSeconds;
			ResetRunDateTime();
		}

		private void ResetRunDateTime()
		{
			RunDateTime = DateTime.UtcNow;
			RunDateTime.AddMilliseconds(delayMillis);
		}

		public void Run()
		{
			state = DelayedOperationState.Running;

			do 
			{
				isRestart = false;
				Start();
			}
			while (isRestart);

			state = DelayedOperationState.Completed;
		}

		public abstract void Start();

		public void Cancel()
		{
			state = DelayedOperationState.Canceled;
		}

		public void ExtendWaitOrRestart()
		{
			if (state == DelayedOperationState.Queued)
			{
				ResetWait();
				Console.WriteLine("Extending wait period.");
			}
			else if (state == DelayedOperationState.Running)
			{
				Restart();
			}
		}

		public void ResetWait()
		{
			ResetRunDateTime();
		}

		public void Restart()
		{
			isRestart = true;
		}

		public override bool Equals(Object obj)
	    {
	        // If parameter is null return false.
	        if ((object)obj == null)
	        {
	            return false;
	        }

	        // If parameter cannot be cast to DelayedOperation return false.
	        AbstractOperation op = obj as AbstractOperation;
	        if ((object)op == null)
	        {
	            return false;
	        }

	        // Return true if the fields match:
	        return Equals(op);
	    }

	    public bool Equals(AbstractOperation op)
	    {
	        // If parameter is null return false:
	        if ((object)op == null)
	        {
	            return false;
	        }

	        // Return true if the operation types match:
	        return OperationType.Equals(op.OperationType);
	    }

	    public override int GetHashCode()
	    {
	        return OperationType.GetHashCode();
	    }
	}
}
