using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace pms.DataModel.FolderScanning
{
	abstract class ScanOperation
	{
		public ScanOperation(int secondsDelay)
		{
			Thread.Sleep(secondsDelay);
		}

		public bool isRestart()
		{
			return false;
		}
	}
}
