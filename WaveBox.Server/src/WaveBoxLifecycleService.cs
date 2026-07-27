using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using WaveBox.Core.Extensions;

namespace WaveBox {
    // Hosted service that owns WaveBox startup/shutdown, replacing the old ServiceBase-derived WaveBoxService
    public class WaveBoxLifecycleService : IHostedService {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(WaveBoxLifecycleService));

        private WaveBoxMain wavebox;

        public Task StartAsync(CancellationToken cancellationToken) {
            logger.IfInfo("Initializing WaveBox");

            // Detect operating system
            ServerInfo.OS = ServerUtility.DetectOS();

            // Store version
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            ServerInfo.BuildVersion = String.Format("{0}.{1}.{2}.{3}", assembly.Version.Major, assembly.Version.Minor, assembly.Version.Build, assembly.Version.Revision);

            // Build date detection
            ServerInfo.BuildDate = ServerUtility.GetBuildDate();
            logger.IfInfo("BuildDate timestamp: " + ServerInfo.BuildDate.ToUnixTime());

            // Get start up time
            ServerInfo.StartTime = DateTime.UtcNow;

            // Create WaveBox's temporary folder
            if (!Directory.Exists(ServerInfo.TempFolder)) {
                Directory.CreateDirectory(ServerInfo.TempFolder);
                logger.IfInfo("Created temp folder: " + ServerInfo.TempFolder);
            }

            // Register API handlers (previously done by the legacy HttpService on startup)
            Core.Injection.Get<Server.IApiHandlerFactory>().Initialize();

            this.wavebox = new WaveBoxMain();
            WaveBoxMain.Start();

            logger.IfInfo("Started!");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            logger.IfInfo("Stopping...");

            // Destroy temp folder
            if (Directory.Exists(ServerInfo.TempFolder)) {
                int i = 0;
                foreach (string f in Directory.GetFiles(ServerInfo.TempFolder)) {
                    File.Delete(f);
                    i++;
                }
                Directory.Delete(ServerInfo.TempFolder);
                logger.IfInfo("Deleted temp folder: " + ServerInfo.TempFolder + " (" + i + " files)");
            }

            // Stop the server
            if (this.wavebox != null) {
                WaveBoxMain.Stop();
                this.wavebox = null;
            }

            logger.IfInfo("Stopped!");
            return Task.CompletedTask;
        }
    }
}
