using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using WaveBox.TcpServer;

namespace WaveBox.TcpServer.Http
{
	public class HttpServer : AbstractTcpServer
	{
		// Name of TCP service
		public override string Name { get { return "HTTP"; } }

		/// <summary>
		/// Abstract constructor for TCP server
		/// <summary>
		public HttpServer(int port) : base(port)
		{
			IsRequired = true;
		}

		/// <summary>
		/// Delegate TCP connection to appropriate TCP server
		/// <summary>
		public override void AcceptClientCallback(IAsyncResult result)
		{
			try
			{
				Listener.BeginAcceptTcpClient(AcceptClientCallback, null);
				TcpClient s = Listener.EndAcceptTcpClient(result);
				HttpProcessor processor = new HttpProcessor(s, this);
				processor.process();
			}
			catch
			{
			}
		}
	}
}
