using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using Bend.Util;
using System.Threading;
using MediaFerry.DataModel.Singletons;
using System.Diagnostics;

namespace MediaFerry
{
	class Program
	{
		static void Main(string[] args)
		{
			int httpPort = 8080;
			// instantiate singletons
			var database = Database.Instance;
			var settings = Settings.Instance;

			var sw = new Stopwatch();
			Console.WriteLine("Scanning and shit...");
			sw.Start();
			var filemanager = FileManager.Instance;
			sw.Stop();
			Console.WriteLine("Scan took {0} seconds", sw.ElapsedMilliseconds / 1000);

			// start http server
			Console.Write("Starting HTTP server... ");
			try
			{
				var http = new PmsHttpServer(httpPort);
				Console.WriteLine("done.");
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
	}
}
