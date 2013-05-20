using System;
using WaveBox.Static;
using WaveBox.OperationQueue;
using WaveBox.SessionManagement;

namespace WaveBox
{
	public class SessionScrubOperation : IDelayedOperation
	{
		// Initialize operation state
		private DelayedOperationState state = DelayedOperationState.None;

		// Name of operation
		private string operationType = "SessionScrub";

		// Delay
		private int originalDelayInMinutes = 0;

		// Return state of operation
		public DelayedOperationState State 
		{ 
			get { return state; }
		}

		// Get the next run DateTime object
		public DateTime RunDateTime { get; set; }

		// Check if the operation is ready
		public bool IsReady 
		{ 
			get 
			{
				if (DateTime.Now >= RunDateTime)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		// Access operation name
		public string OperationType { get { return operationType; } }

		/// <summary>
		/// Run the session scrubbing operation
		/// </summary>
		public void Run()
		{
			SessionScrub.Start();

			// Queue up the next one
			SessionScrub.Queue.queueOperation(new SessionScrubOperation(Settings.SessionScrubInterval));
		}

		// No need to cancel this operation
		public void Cancel()
		{

		}

		// ... Or restart it
		public void Restart()
		{

		}

		/// <summary>
		/// Reset the operation to its original delay
		/// </summary>
		public void ResetWait()
		{
			RunDateTime = DateTime.Now.AddSeconds(originalDelayInMinutes);
		}

		/// <summary>
		/// Set up the operation to run again at next interval
		/// </summary>
		public SessionScrubOperation(int minutesDelay)
		{
			RunDateTime = DateTime.Now.AddMinutes(minutesDelay);
			originalDelayInMinutes = minutesDelay;
		}
	}
}
