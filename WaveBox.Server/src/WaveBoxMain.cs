using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Model;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Transcoding;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model.Repository;

namespace WaveBox {
    class WaveBoxMain {
        // Logger
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        /// <summary>
        /// The main instance of WaveBox which runs the server.  Creates necessary directories, initializes
        /// database and settings, and starts all associated services.
        /// </summary>
        public void Start() {
            logger.IfInfo("Initializing WaveBox " + ServerInfo.BuildVersion + " on " + ServerInfo.OS.ToDescription() + " platform...");

            // Create directory for WaveBox's root path, if it doesn't exist
            string rootDir = ServerUtility.RootPath();
            if (!Directory.Exists(rootDir)) {
                Directory.CreateDirectory(rootDir);
            }

            // Create directory for WaveBox Web UI themes, if it doesn't exist
            string themeDir = ServerUtility.ExecutablePath() + "themes/";
            if (!Directory.Exists(themeDir)) {
                Directory.CreateDirectory(themeDir);
            }

            // Perform initial setup of Settings, Database
            Injection.Get<IDatabase>().DatabaseSetup();
            Injection.Get<IServerSettings>().SettingsSetup();

            // Start services
            try {
                // Initialize factory, so it can register all services for deployment
                ServiceFactory.Initialize();

                // Start user defined services
                if (Injection.Get<IServerSettings>().Services != null) {
                    ServiceManager.AddList(Injection.Get<IServerSettings>().Services);
                } else {
                    logger.Warn("No services specified in configuration file!");
                }

                ServiceManager.StartAll();
            } catch (Exception e) {
                logger.Warn("Could not start one or more WaveBox services, please check services in your configuration");
                logger.Warn(e);
            }

            // Temporary: create test and admin user
            Injection.Get<IUserRepository>().CreateUser("test", "test", Role.User, null);
            Injection.Get<IUserRepository>().CreateUser("admin", "admin", Role.Admin, null);

            return;
        }

        /// <summary>
        /// Stop the WaveBox main
        /// </summary>
        public void Stop() {
            // Stop all running services
            ServiceManager.StopAll();
            ServiceManager.Clear();
        }

        /// <summary>
        /// Restart the WaveBox main
        /// </summary>
        public void Restart() {
            this.Stop();
            this.Start();
        }
    }
}
