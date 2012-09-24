using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WaveBox.ApiHandler;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace WaveBox.Http
{
	public class HttpServer 
	{
		protected int Port { get; set; }
		private TcpListener Listener { get; set; }
		private bool IsActive { get; set; }
	   
		public HttpServer(int port) 
		{
			IsActive = true;
			Port = port;
		}

		public void Listen()
		{
			Listener = new TcpListener(IPAddress.Any, Port);
			try
			{
				Listener.Start();
			}
			catch (System.Net.Sockets.SocketException e)
			{
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
				{
					Console.WriteLine("[HTTPSERVER] Socket already in use, is WaveBox already running?");
				}
				else
					Console.WriteLine("[HTTPSERVER(4)] " + e);

				Environment.Exit(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine("[HTTPSERVER(5)] " + e);
				Environment.Exit(-1);
			}

			Console.WriteLine("[HTTPSERVER] HTTP server started");
			while (IsActive) 
			{                
				TcpClient s = Listener.AcceptTcpClient();
				//TcpClient d = listener.BeginAcceptTcpClient
				HttpProcessor processor = new HttpProcessor(s, this);
				Thread thread = new Thread(new ThreadStart(processor.process));
				thread.Start();
				Thread.Sleep(1);
			}
		}
	}
}
