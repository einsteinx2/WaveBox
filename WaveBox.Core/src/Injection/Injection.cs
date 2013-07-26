using System;
using Ninject;
using WaveBox.Core;

namespace WaveBox.Core
{
	public static class Injection
	{
		public static IKernel Kernel { get; private set; }

		static Injection()
		{
			Kernel = new StandardKernel(new CoreModule());
		}
	}
}
