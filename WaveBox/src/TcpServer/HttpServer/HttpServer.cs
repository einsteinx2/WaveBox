using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
