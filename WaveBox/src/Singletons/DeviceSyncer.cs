using System;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;
using SuperWebSocket;
using SuperWebSocket.SubProtocol;
using System.Reflection;

namespace WaveBox.Singletons
{
	static class DeviceSyncer
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static WebSocketServer webSocketServer;

		public static void Start()
		{
			Stop();

			webSocketServer = new WebSocketServer();
			webSocketServer.Setup(new RootConfig(), new ServerConfig
			                      {
				Port = (Settings.WsPort),
				Ip = "Any",
				MaxConnectionNumber = 100,
				Mode = SocketMode.Async,
				Name = "WaveBox"
			}, SocketServerFactory.Instance);

			webSocketServer.NewSessionConnected += HandleNewSessionConnected;
			webSocketServer.NewMessageReceived += HandleNewMessageReceived;
			webSocketServer.SessionClosed += HandleSessionClosed;

			webSocketServer.Start();
		}

		public static void Stop()
		{
			if (webSocketServer != null)
			{
				webSocketServer.Stop();
				webSocketServer.Dispose();
				webSocketServer = null;
			}
		}

		private static void HandleNewSessionConnected (WebSocketSession session)
		{
			logger.Info("WebSocketServer - New session connected: " + session);
			session.SendResponse("WebSocketServer - New session connected: " + session);
		}

		private static void HandleNewMessageReceived (WebSocketSession session, string e)
		{
			logger.Info("WebSocketServer - New message received: " + e);
			session.SendResponse("WebSocketServer - New message received: " + e);
		}

		private static void HandleSessionClosed (WebSocketSession session, CloseReason e)
		{
			logger.Info("WebSocketServer - Session closed");
			session.SendResponse("WebSocketServer - Session closed");
		}
	}
}

