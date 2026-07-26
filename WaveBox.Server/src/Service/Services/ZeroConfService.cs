using System;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Core;

namespace WaveBox.Service.Services {
    // Stub: the Mono.Zeroconf library died with Mono.  The service name stays registered so that
    // existing wavebox.conf files listing "zeroconf" don't error, but mDNS advertising is disabled.
    // If advertising matters again, shell out to dns-sd/avahi-publish or use an mDNS library.
    public class ZeroConfService : IService {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ZeroConfService));

        public string Name { get { return "zeroconf"; } set { } }

        public bool Required { get { return false; } set { } }

        public bool Running { get; set; }

        public bool Start() {
            logger.Warn("ZeroConf (mDNS) advertising is no longer supported and has been disabled; remove 'zeroconf' from the services list in wavebox.conf to silence this warning");
            return true;
        }

        public bool Stop() {
            return true;
        }
    }
}
