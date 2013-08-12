using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Core.Extensions;
using WaveBox.Service.Services;

namespace WaveBox.Service
{
	public static class ServiceManager
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// List of all services maintained by the manager
		private static IList<IService> Services = new List<IService>();

		/// <summary>
		/// Add a new service, by name, to the manager, optionally starting it automatically
		/// </summary>
		public static bool Add(string name, bool autostart = false)
		{
			// Ensure lowercase names
			name = name.ToLower();

			// Check if service is disabled
			if (name[0] == '!')
			{
				logger.IfInfo("Skipping disabled service: " + name);
				return true;
			}

			// Check if service is already present in list
			if (Services.Any(x => x.Name == name))
			{
				logger.IfInfo("Skipping duplicate service: " + name);
				return false;
			}

			// Generate service from name
			IService service = ServiceFactory.CreateService(name);

			// Ensure service was valid
			if ((object)service == null)
			{
				logger.Error("Unknown service: " + name);
				return false;
			}

			// Add service to list
			Services.Add(service);

			// Autostart if requested
			if (autostart)
			{
				if (!Start(service))
				{
					logger.Error("Failed to autostart service: " + service.Name);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Add a list of services, by name, to the manager, optionally autostarting all of them
		/// </summary>
		public static bool AddList(IList<string> names, bool autostart = false)
		{
			bool success = true;

			// Add all services in list
			foreach (string n in names)
			{
				if (!Add(n, autostart))
				{
					logger.Error("Failed to register new service from list: " + n);
					success = false;
				}
			}

			return success;
		}

		/// <summary>
		/// Clear all currently registered services
		/// </summary>
		public static bool Clear()
		{
			logger.IfInfo("Clearing all registered services...");
			Services.Clear();

			return true;
		}

		/// <summary>
		/// Return the registered instance of a running IService object, by its name
		/// <summary>
		public static IService GetInstance(string name)
		{
			// Ensure service is already present in list
			if (!Services.Any(x => x.Name == name))
			{
				return null;
			}

			// Return this object from the list
			return Services.Where(x => x.Name == name).Single();
		}

		/// <summary>
		/// Return string list of registered services
		/// </summary>
		public static IList<string> GetServices()
		{
			return Services.Select(x => x.Name).ToList();
		}

		/// <summary>
		/// Attempt to restart all currently registered services
		/// </summary>
		public static bool RestartAll()
		{
			if (!StopAll())
			{
				return false;
			}

			if (!StartAll())
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Attempt to start all currently registered services
		/// </summary>
		public static bool StartAll()
		{
			logger.IfInfo("Starting all registered services...");
			bool success = true;

			// Add all required services
			foreach (string s in ServiceFactory.RequiredServices)
			{
				if (!Services.Any(x => x.Name == s))
				{
					Add(s);
				}
			}

			// Start all services
			foreach (IService s in Services)
			{
				if (!Start(s))
				{
					logger.Error("Failed to start service from list: " + s.Name);
					success = false;
				}
			}

			if (success)
			{
				logger.IfInfo("All registered services started!");
			}
			else
			{
				logger.Error("Failed to start some services!");
			}

			return success;
		}

		/// <summary>
		/// Attempt to stop all currently registered services
		/// </summary>
		public static bool StopAll()
		{
			logger.IfInfo("Stopping all registered services...");
			bool success = true;

			// Stop all services
			foreach (IService s in Services)
			{
				if (!Stop(s))
				{
					logger.Error("Failed to stop service from list: " + s.Name);
					success = false;
				}
			}

			if (success)
			{
				logger.IfInfo("All registered services stopped!");
			}
			else
			{
				logger.Error("Failed to stop some services!");
			}

			return success;
		}

		/// <summary>
		/// Start and log the specified service
		/// </summary>
		private static bool Start(IService service)
		{
			bool success = false;
			if ((object)service == null)
			{
				logger.Error("Service is null, cannot start");
				return false;
			}

			// Check if it's already running
			if (service.Running)
			{
				logger.IfInfo("  - Already running: " + service.Name);
				return true;
			}

			if (service.Start())
			{
				string output = "  - Started: " + service.Name;
				if (service.Required)
				{
					output += " *";
				}
				logger.IfInfo(output);
				service.Running = true;
				success = true;
			}
			else
			{
				logger.Error("  ! Failed to start: " + service.Name);

				// Check if service was required
				if (service.Required)
				{
					logger.Error("Service " + service.Name + " is required, exiting now!");
					Environment.Exit(-1);
				}
			}

			return success;
		}

		/// <summary>
		/// Stop and log the specified service
		/// </summary>
		private static bool Stop(IService service)
		{
			bool success = false;
			if ((object)service == null)
			{
				logger.Error("Failed to stop service: " + service.Name);
				return false;
			}

			// Check if it's already stopped
			if (!service.Running)
			{
				logger.IfInfo("  - Already stopped: " + service.Name);
				return true;
			}

			if (service.Stop())
			{
				logger.IfInfo("  - Stopped: " + service.Name);
				service.Running = false;
				success = true;
			}
			else
			{
				logger.Error("  ! Failed to stop: " + service.Name);
			}

			return success;
		}
	}
}
