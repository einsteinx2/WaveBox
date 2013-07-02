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
using Microsoft.Owin.Hosting;
using Mono.Unix.Native;
using Mono.Unix;
using Mono.Zeroconf;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.DeviceSync;
using WaveBox.Model;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Transcoding;

namespace WaveBox
{
	class WaveBoxMain
	{
		// Logger
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The main program for WaveBox.  Launches the HTTP server, initializes settings, creates default user,
		/// begins file scan, and then sleeps forever while other threads handle the work.
		/// </summary>
		public void Start()
		{
			if (logger.IsInfoEnabled) logger.Info("Initializing WaveBox " + WaveBoxService.BuildVersion + " on " + WaveBoxService.Platform + " platform...");

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

			// Perform initial setup of Settings, Database
			Injection.Kernel.Get<IDatabase>().DatabaseSetup();
			Injection.Kernel.Get<IServerSettings>().SettingsSetup();

			// Start services
			try
			{
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

			// Start the SignalR server for real time device state syncing
			try
			{
				WebApplication.Start<DeviceSyncStartup>("http://localhost:" + Injection.Kernel.Get<IServerSettings>().WsPort + "/");
			}
			catch (Exception e)
			{
				logger.Warn("Could not start WaveBox SignalR server, please check wsPort in your configuration");
				logger.Warn(e);
			}

			// Temporary: create test user
			new User.Factory().CreateUser("test", "test", null);

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
