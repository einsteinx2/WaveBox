using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Mono.Unix.Native;
using Mono.Unix;
using Mono.Zeroconf;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Transcoding;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model.Repository;

namespace WaveBox
{
	class WaveBoxMain
	{
		// Logger
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The main instance of WaveBox which runs the server.  Creates necessary directories, initializes
		/// database and settings, and starts all associated services.
		/// </summary>
		public void Start()
		{
			logger.IfInfo("Initializing WaveBox " + WaveBoxService.BuildVersion + " on " + WaveBoxService.OS.ToDescription() + " platform...");

			// Initialize ImageMagick
			try
			{
				ImageMagickInterop.WandGenesis();
			}
			catch (Exception e)
			{
				logger.Error("Error loading ImageMagick DLL:", e);
			}

			// Create directory for WaveBox's root path, if it doesn't exist
			string rootDir = ServerUtility.RootPath();
			if (!Directory.Exists(rootDir))
			{
				Directory.CreateDirectory(rootDir);
			}

			// Create directory for WaveBox Web UI themes, if it doesn't exist
			string themeDir = ServerUtility.ExecutablePath() + "themes/";
			if (!Directory.Exists(themeDir))
			{
				Directory.CreateDirectory(themeDir);
			}

			// Perform initial setup of Settings, Database
			Injection.Kernel.Get<IDatabase>().DatabaseSetup();
			Injection.Kernel.Get<IServerSettings>().SettingsSetup();

			// Start services
			try
			{
				// Initialize factory, so it can register all services for deployment
				ServiceFactory.Initialize();

				// Start user defined services
				if (Injection.Kernel.Get<IServerSettings>().Services != null)
				{
					ServiceManager.AddList(Injection.Kernel.Get<IServerSettings>().Services);
				}
				else
				{
					logger.Warn("No services specified in configuration file!");
				}

				ServiceManager.StartAll();
			}
			catch (Exception e)
			{
				logger.Warn("Could not start one or more WaveBox services, please check services in your configuration");
				logger.Warn(e);
			}

			// Temporary: create test and admin user
			Injection.Kernel.Get<IUserRepository>().CreateUser("test", "test", Role.User, null);
			Injection.Kernel.Get<IUserRepository>().CreateUser("admin", "admin", Role.Admin, null);

			return;
		}

		/// <summary>
		/// Stop the WaveBox main
		/// </summary>
		public void Stop()
		{
			// Stop all running services
			ServiceManager.StopAll();
			ServiceManager.Clear();

			// Dispose of ImageMagick
			try
			{
				ImageMagickInterop.WandTerminus();
			}
			catch (Exception e)
			{
				logger.Error("Error loading ImageMagick DLL:", e);
			}
		}

		/// <summary>
		/// Restart the WaveBox main
		/// </summary>
		public void Restart()
		{
			this.Stop();
			this.Start();
		}
	}
}
