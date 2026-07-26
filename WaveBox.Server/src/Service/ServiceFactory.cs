using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Core.Extensions;
using WaveBox.Service.Services;
using WaveBox.Static;

namespace WaveBox.Service {
    public static class ServiceFactory {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ServiceFactory));

        // List of required services which will run at all times ("http" is now Kestrel, managed by the host)
        public static readonly List<string> RequiredServices = new List<string> {"cron", "filemanager", "transcode"};

        // List of registered services (explicit list - reflection scanning is not NativeAOT-compatible)
        private static List<IService> services = new List<IService>();

        /// <summary>
        /// Return the requested IService object which will be managed by ServiceManager
        /// <summary>
        public static IService CreateService(string name) {
            // Any services with this name?  If yes, return service.  If no, return null.
            return services.Any(x => x.Name == name) ? services.Single(x => x.Name == name) : null;
        }

        /// <summary>
        /// Register all available services with the factory
        /// <summary>
        public static void Initialize() {
            services.Clear();
            services.Add(new CronService());
            services.Add(new FileManagerService());
            services.Add(new NatService());
            services.Add(new NowPlayingService());
            services.Add(new TranscodeService());
            services.Add(new ZeroConfService());

            foreach (IService s in services) {
                logger.IfInfo("Registered service: " + s.Name + " -> " + s.GetType());
            }
        }
    }
}
