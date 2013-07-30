using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninject;
using WaveBox.Static;

namespace WaveBox.ApiHandler
{
	public static class ApiHandlerFactory
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// List of API handlers which are keyed and instantiated upon discovery
		private static List<IApiHandler> apiHandlers = new List<IApiHandler>();

		/// <summary>
		/// Return the requested IApiHandler object
		/// <summary>
		public static IApiHandler CreateApiHandler(string name)
		{
			// Any API handlers with this name?  If yes, return it.  If no, return null.
			return apiHandlers.Any(x => x.Name == name) ? apiHandlers.Single(x => x.Name == name) : null;
		}

		/// <summary>
		/// Use reflection to scan for all available IApiHandler-implementing classes, register them as valid API
		/// handlers to be created by factory
		/// <summary>
		public static void Initialize()
		{
			try
			{
				// Grab all available types which implement IApiHandler
				foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IApiHandler))))
				{
					// Discover and instantiate all available apiHandlers
					IApiHandler instance = (IApiHandler)Activator.CreateInstance(t);
					if (logger.IsInfoEnabled) logger.Info("Discovered API handler: " + instance.Name + " -> " + t);
					apiHandlers.Add(instance);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
