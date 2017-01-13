using System;
using Mono.Nat;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Service;
using WaveBox.Static;

namespace WaveBox.Service.Services {
    public enum NatStatus {
        NotInitialized = 0,
        WaitingForDevice = 1,
        DeviceFound = 2,
        PortForwardedSuccessfully = 3,
        PortForwardingFailed = 4
    }

    public class NatService : IService {
        // Logger
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "nat"; } set { } }

        public bool Required { get { return false; } set { } }

        public bool Running { get; set; }

        public NatStatus Status { get; set; }

        private INatDevice Device { get; set; }

        public NatService() {
        }

        public bool Start() {
            this.Status = NatStatus.WaitingForDevice;

            // Hook into the events so you know when a router has been detected or has gone offline
            NatUtility.DeviceFound += DeviceFound;

            // Mono.Nat does never rise this event. The event is there however it is useless.
            // You could remove it with no risk.
            //NatUtility.DeviceLost += DeviceLost;

            // it is hard to say what one should do when an unhandled exception is raised
            // because there isn't anything one can do about it. Probably save a log or ignored it.
            // You assumption is that 'status = PortForwardingFailed' when this event is raised and
            // that is wrong.
            // This event is raised when something was wrong in the discovery process (a thread that
            // is continuely discovering) however it can fail after your portmapping was successfuly
            // created.
            NatUtility.UnhandledException += UnhandledException;

            // Start searching for upnp enabled routers
            NatUtility.StartDiscovery();

            return true;
        }

        public bool Stop() {
            if ((object)Device != null && Status == NatStatus.PortForwardedSuccessfully) {
                // As I said before, this is not okey. Just imagine this scenario:
                // 1. Mono.Nat starts discovering and finds a router
                // 2. You create a port mapping successfully
                // 3. Given you never stopped the discovery process, it continues discovering
                // 4. Two hours later it fails and raises an unhandled exception event
                // 5. You handle the event and set Status = NatStatus.PortForwardingFailed - however there is nothing wrong with the created port mapping
                // 6. When invoked, this method will not delete the mapping!
                Status = NatStatus.NotInitialized;
                Device.DeletePortMap(new Mapping(Protocol.Tcp, Injection.Kernel.Get<IServerSettings>().Port, Injection.Kernel.Get<IServerSettings>().Port));

                // DeletePortMapping can fail so, Status should be set before
            }

            return true;
        }

        private void DeviceFound(object sender, DeviceEventArgs args) {
            logger.IfInfo("Device Found");

            this.Status = NatStatus.DeviceFound;

            // This is the upnp enabled router
            this.Device = args.Device;

            // Create a mapping to forward external port to local port
            try {
                Device.CreatePortMap(new Mapping(Protocol.Tcp, Injection.Kernel.Get<IServerSettings>().Port, Injection.Kernel.Get<IServerSettings>().Port));
                this.Status = NatStatus.PortForwardedSuccessfully;
            } catch (Exception e) {
                this.Status = NatStatus.PortForwardingFailed;
                logger.Error("Port mapping failed", e);
            }
        }

        // This is not funny, all developers in all the projects that I saw, spend time thinking
        // about what they should do when a device is lost however the event is a bluff.
        private void DeviceLost(object sender, DeviceEventArgs args) {
            this.Status = NatStatus.PortForwardingFailed;

            INatDevice device = args.Device;

            logger.IfInfo("Device Lost");
            logger.IfInfo("Type: " + device.GetType().Name);
        }

        private void UnhandledException(object sender, UnhandledExceptionEventArgs args) {
            this.Status = NatStatus.PortForwardingFailed; // you have a problem here

            logger.Error("Unhandled exception: " + args);
        }
    }
}
