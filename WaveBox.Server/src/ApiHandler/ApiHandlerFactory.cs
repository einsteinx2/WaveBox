using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninject;
using WaveBox.Core.Extensions;
using WaveBox.Server;
using WaveBox.Static;

namespace WaveBox.ApiHandler
{
	public class ApiHandlerFactory : IApiHandlerFactory
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// List of API handlers which are keyed and instantiated upon discovery
		private List<IApiHandler> apiHandlers;

		/// <summary>
		/// Return the requested IApiHandler object
		/// <summary>
		public IApiHandler CreateApiHandler(string name)
		{
			// Any API handlers with this name?  If yes, return it.  If no, return null.
			return this.apiHandlers.SingleOrDefault(x => x.Name == name);
		}

		/// <summary>
		/// Use reflection to scan for all available IApiHandler-implementing classes, register them as valid API
		/// handlers to be created by factory
		/// <summary>
		public void Initialize()
		{
			try
			{
				// Initialize list
				apiHandlers = new List<IApiHandler>();

				// Grab all available types which implement IApiHandler
				foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IApiHandler))))
				{
					// Discover and instantiate all available apiHandlers
					IApiHandler instance = (IApiHandler)Activator.CreateInstance(t);
					logger.IfInfo("Discovered API: " + instance.Name + " -> " + t);
					this.apiHandlers.Add(instance);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
