using System;

namespace WaveBox.Service
{
	public interface IService
	{
		// Name of service which is being managed
		string Name { get; set; }

		// Service control methods
		bool Start();
		bool Stop();
	}
}
