using System;
using WaveBox.OperationQueue;
using WaveBox.PodcastManagement;
using WaveBox.DataModel.Singletons;
using NLog;

namespace WaveBox
{
	public class FeedCheckOperation : IDelayedOperation
	{
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		// property backing ivars
		DelayedOperationState state = DelayedOperationState.None;
		string operationType = "PodcastFeedCheck";
		int originalDelayInMinutes = 0;

		public DelayedOperationState State 
		{ 
			get { return state; }
		}
		public DateTime RunDateTime { get; set; }
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
		public string OperationType { get { return operationType; } }
		
		public void Run()
		{
			var podcasts = Podcast.ListOfStoredPodcasts();
			foreach (var podcast in podcasts)
			{
				podcast.DownloadNewEpisodes();
			}
			PodcastManagement.DownloadQueue.FeedChecks.queueOperation(new FeedCheckOperation(Settings.PodcastCheckInterval));
		}

		public void Cancel()
		{
			DownloadQueue.CancelAll();
		}

		public void ResetWait()
		{
			RunDateTime = DateTime.Now.AddSeconds(originalDelayInMinutes);
		}

		public void Restart()
		{
			DownloadQueue.CancelAll();
			Run();
		}

		public FeedCheckOperation(int minutesDelay)
		{
			RunDateTime = DateTime.Now.AddMinutes(minutesDelay);
			originalDelayInMinutes = minutesDelay;
		}
	}
}

