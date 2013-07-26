using System;
using Ninject;
using WaveBox.Core.OperationQueue;
using WaveBox.Static;

namespace WaveBox.Service.Services.Cron
{
	public class UserPurgeOperation : IDelayedOperation
	{
		// Initialize operation state
		private DelayedOperationState state = DelayedOperationState.None;

		// Name of operation
		private string operationType = "UserPurge";

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
		/// Run the UserPurge operation
		/// </summary>
		public void Run()
		{
			UserPurge.Start();

			// Queue up the next one in 10 minutes
			UserPurge.Queue.queueOperation(new UserPurgeOperation(10));
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
		public UserPurgeOperation(int minutesDelay)
		{
			RunDateTime = DateTime.Now.AddMinutes(minutesDelay);
			originalDelayInMinutes = minutesDelay;
		}
	}
}
