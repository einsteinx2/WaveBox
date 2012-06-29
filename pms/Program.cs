using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using Bend.Util;
using System.Threading;
using MediaFerry.DataModel.Singletons;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaFerry
{
	class Program
	{

		static void Main(string[] args)
		{
			// define run port
			int httpPort = 8080;

			// register application kill notifier
			Console.WriteLine("Registering shutdown hook...");
			_handler += new EventHandler(Handler);
			SetConsoleCtrlHandler(_handler, true);
			
			var database = Database.Instance;
			var settings = Settings.Instance;

			var sw = new Stopwatch();
			Console.WriteLine("Scanning media directories...");
			sw.Start();
			var filemanager = FileManager.Instance;
			sw.Stop();
			Console.WriteLine("Scan took {0} seconds", sw.ElapsedMilliseconds / 1000);

			// start http server
			Console.Write("Starting HTTP server... ");
			try
			{
				var http = new PmsHttpServer(httpPort);
				Console.WriteLine("done.\r\n\r\n");
				http.listen();
			}

			catch (System.Net.Sockets.SocketException e)
			{
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
				{
					Console.WriteLine("ERROR: Socket already in use.  Ensure that PMS is not already running.");
				}

				else Console.WriteLine("ERROR: " + e.Message);
				Environment.Exit(-1);
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Environment.Exit(-1);
			}

			// sleep the main thread so we can go about handling api calls and stuff on other threads.
			Thread.Sleep(Timeout.Infinite);
		}

		[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

		private delegate bool EventHandler(CtrlType sig);
		static EventHandler _handler;

		enum CtrlType
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT = 1,
			CTRL_CLOSE_EVENT = 2,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT = 6
		}

		private static bool Handler(CtrlType sig)
		{
			Console.WriteLine("Executing shutdown hook!");
			SqlCeConnection dbconn = Database.getDbConnection();
			dbconn.Close();

			if (dbconn.State == System.Data.ConnectionState.Closed)
			{
				Console.WriteLine("Database connection successfully closed");
			}

			else Console.WriteLine("Database connection failed to close");

			return true;
		}

	}
}
