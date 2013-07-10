using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninject;
using WaveBox.Core.Injected;
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
		private static Dictionary<string, IService> services = new Dictionary<string, IService>();

		/// <summary>
		/// Return the requested IService object which will be managed by ServiceManager
		/// <summary>
		public static IService CreateService(string name)
		{
			return services.ContainsKey(name) ? services[name] : null;
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
				var types = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IService)));
				foreach (Type t in types)
				{
					// Instantiate the instance to grab its name without further reflection
					IService instance = (IService)Activator.CreateInstance(t);

					// Only bother keeping instance if service is RequiredServices or specified active in settings
					if (RequiredServices.Contains(instance.Name) || Injection.Kernel.Get<IServerSettings>().Services.Contains(instance.Name))
					{
						logger.Info("Discovered service: " + instance.Name + " -> " + t);
						services.Add(instance.Name, instance);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
