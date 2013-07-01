using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace WaveBox.TcpServer
{
	public abstract class AbstractTcpServer
	{
		// log4net logger
		protected static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Name of TCP service
		public abstract string Name { get; }

		// Port for TCP server
		protected int Port { get; set; }

		// Determines if server required for WaveBox to run
		protected bool IsRequired { get; set; }

		// Determines if server is currently active
		protected bool IsActive { get; set; }

		// TCP connection listener for server
		protected TcpListener Listener { get; set; }

		/// <summary>
		/// Abstract constructor for TCP server
		/// <summary>
		protected AbstractTcpServer(int port)
		{
			IsActive = true;
			Port = port;
		}

		/// <summary>
		/// Begin listening on this server
		/// <summary>
		public void Listen()
		{
			try
			{
				// Bind to local port and attempt start
				Listener = new TcpListener(IPAddress.Any, Port);
				Listener.Start();

				// Start accepting TCP clients
				Listener.BeginAcceptTcpClient(AcceptClientCallback, null);
				logger.Info("TCP server started: " + Name + ":" + Port);
			}
			// Catch socket exceptions
			catch (System.Net.Sockets.SocketException e)
			{
				// Port already in use
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
				{
					logger.Error("TCP port " + Port + " already in use, is WaveBox " + Name + " or another service already running?");
				}
				// Other errors
				else
				{
					logger.Error(e);
				}

				logger.Error("Could not start TCP server: " + Name + ":" + Port);

				// Fail on socket error, if service is required (HTTP)
				if (IsRequired)
				{
					logger.Error("TCP server " + Name + ":" + Port + " is required to run WaveBox, exiting now!");
					Environment.Exit(-1);
				}
			}
			// Catch generic exception
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		/// <summary>
		/// Stop an active TCP server
		/// <summary>
		public void Stop()
		{
			if ((object)Listener != null)
			{
				try
				{
					Listener.Stop();
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// Delegate TCP connection to appropriate TCP server
		/// <summary>
		public abstract void AcceptClientCallback(IAsyncResult result);
	}
}
