using System.Net;
using System.Net.Sockets;

namespace WaveBox.TestFixtures {
    public static class TcpPort {
        /// <summary>
        /// Asks the OS for a free TCP port by binding to port 0, then releases it. The small window
        /// between release and reuse is acceptable for test isolation purposes.
        /// </summary>
        public static int GetFree() {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
