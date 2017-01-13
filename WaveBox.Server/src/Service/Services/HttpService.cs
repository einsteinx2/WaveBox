using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Ninject;
using WaveBox.ApiHandler;
using WaveBox.Server;
using WaveBox.Service;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Core;

namespace WaveBox.Service.Services {
    class HttpService : IService {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "http"; } set { } }

        public bool Required { get { return true; } set { } }

        public bool Running { get; set; }

        private int Port { get { return Injection.Kernel.Get<IServerSettings>().Port; } set { } }

        private TcpListener Listener { get; set; }

        public HttpService() {
        }

        public bool Start() {
            try {
                // Discover and register all API handlers
                Injection.Kernel.Get<IApiHandlerFactory>().Initialize();

                // Bind to local port and attempt start
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();

                // Start accepting TCP clients
                Listener.BeginAcceptTcpClient(AcceptClientCallback, null);
            }
            // Catch socket exceptions
            catch (System.Net.Sockets.SocketException e) {
                // Port already in use
                if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse") {
                    logger.Error("TCP port " + Port + " already in use, is WaveBox " + Name + " or another service already running?");
                }
                // Other errors
                else {
                    logger.Error(e);
                }

                return false;
            }
            // Catch generic exception
            catch (Exception e) {
                logger.Error(e);
                return false;
            }

            return true;
        }

        public bool Stop() {
            if ((object)Listener != null) {
                try {
                    Listener.Stop();
                } catch {
                }
            }

            return true;
        }

        /// <summary>
        /// Delegate TCP connection to appropriate TCP server
        /// <summary>
        private void AcceptClientCallback(IAsyncResult result) {
            try {
                Listener.BeginAcceptTcpClient(AcceptClientCallback, null);
                TcpClient s = Listener.EndAcceptTcpClient(result);
                HttpProcessor processor = new HttpProcessor(s);
                processor.Process();
            } catch {
            }
        }
    }
}
