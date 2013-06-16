using System;
using Ninject;

namespace WaveBox.Static
{
	public static class Injection
	{
		public static IKernel Kernel { get; private set; }

		static Injection()
		{
			Kernel = new StandardKernel();
		}
	}
}
