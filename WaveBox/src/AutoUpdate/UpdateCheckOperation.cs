using System;
using WaveBox.OperationQueue;

namespace WaveBox.AutoUpdate
{
	public class UpdateCheckOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "UpdateCheckOperation"; } }

		private string UpdateJsonUrl { get; set; }

		public UpdateCheckOperation(string updateJsonUrl, int delayMilliSeconds) : base(delayMilliSeconds)
		{
			UpdateJsonUrl = updateJsonUrl;
		}

		public override void Start()
		{

		}
	}
}

