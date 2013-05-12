using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using WaveBox.Model;
using WaveBox.Singletons;

namespace WaveBox.TcpServer.Mpd
{
	public class MpdProcessor
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public TcpClient Socket { get; set; }
		public MpdServer Srv { get; set; }

		private NetworkStream Net;
		//private StreamReader Reader;
		private StreamWriter Writer;

		public MpdProcessor(TcpClient s, MpdServer srv) 
		{
			Socket = s;
			Srv = srv;
		}

		public void Process()
		{
			// Grab socket stream, open I/O streams
			Net = Socket.GetStream();
			//Reader = new StreamReader(Net, Encoding.UTF8);
			Writer = new StreamWriter(new BufferedStream(Net), new UTF8Encoding(false));
			Writer.AutoFlush = true;

			if (logger.IsInfoEnabled) logger.Info("Got mpd connection");

			// Send greeting to client
			Writer.WriteLine(MpdResponse.Greeting);

		}
	}
}
