using System;

namespace WaveBox.ApiHandler
{
	public interface IApiHandler
	{
		// Ensure all API handlers have a name
		string Name { get; set; }

		// Ensure all API handlers implement a processing function
		void Process();
	}
}
