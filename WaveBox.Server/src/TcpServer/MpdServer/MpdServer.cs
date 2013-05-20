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

namespace WaveBox.TcpServer.Mpd
{
	public class MpdServer : AbstractTcpServer
	{
		// Name of TCP service
		public override string Name { get { return "MPD"; } }

		/// <summary>
		/// Constructor for MPD server
		/// <summary>
		public MpdServer(int port) : base(port)
		{
			IsRequired = false;
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
				MpdProcessor processor = new MpdProcessor(s, this);
				processor.Process();
			}
			catch
			{

			}
		}
	}
}
