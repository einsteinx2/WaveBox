using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.ApiHandler
{
	// This interface is implemented by all API handlers, to ensure that the Process() method is implemented
	interface IApiHandler
	{
		void Process();
	}
}
