using System;
using Ninject;

namespace WaveBox.Core.Static
{
	public static class Injection
	{
		public static IKernel Kernel { get; set; }

		static Injection()
		{
			Kernel = new StandardKernel();
		}
	}
}
