using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Service.Services;
using WaveBox.Static;

namespace WaveBox.Service
{
	public static class ServiceFactory
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// List of RequiredServices services which will run at all times
		public static readonly List<string> RequiredServices = new List<string>{"cron", "filemanager", "http", "transcode"};

		// List of services which are keyed and instantiated upon discovery
		private static List<IService> services = new List<IService>();

		/// <summary>
		/// Return the requested IService object which will be managed by ServiceManager
		/// <summary>
		public static IService CreateService(string name)
		{
			// Any services with this name?  If yes, return service.  If no, return null.
			return services.Any(x => x.Name == name) ? services.Single(x => x.Name == name) : null;
		}

		/// <summary>
		/// Use reflection to scan for all available IService-implementing classes, register them as valid services
		/// to be created by factory
		/// <summary>
		public static void Initialize()
		{
			try
			{
				// Grab all available types which implement IService
				foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IService))))
				{
					// Discover and instantiate all available services
					IService instance = (IService)Activator.CreateInstance(t);
					if (logger.IsInfoEnabled) logger.Info("Discovered service: " + instance.Name + " -> " + t);
					services.Add(instance);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
