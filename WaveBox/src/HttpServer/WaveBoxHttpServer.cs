using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WaveBox.ApiHandler;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.HttpServer
{
	public class WaveBoxHttpServer : HttpServer
	{
		public WaveBoxHttpServer(int port) : base(port)
		{
		}

		public override void HandleGETRequest(HttpProcessor processor)
		{
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(processor.HttpUrl, processor);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public override void HandlePOSTRequest(HttpProcessor processor, StreamReader inputData)
		{
			string data = processor.HttpUrl + "?" + inputData.ReadToEnd();
			Console.WriteLine("[HTTPSERVER] POST request: {0}", data);
			
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(data, processor);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public static void sendJson(HttpProcessor processor, string json)
		{
			processor.WriteSuccess();
			processor.OutputStream.Write(json);
		}

		public static void sendFile(HttpProcessor processor, FileStream fs, int startOffset)
		{
			FileInfo fsinfo = null;
			if (fs == null)
			{
				return;
			}

			else fsinfo = new FileInfo(fs.Name);

			long fileLength = 0;

			try
			{
				fileLength = fs.Length - startOffset;
			}

			catch (Exception e)
			{
				Console.WriteLine("[SENDFILE] " + e.ToString());
			}

			// new http header object
			WaveBoxHttpHeader h = new WaveBoxHttpHeader(WaveBoxHttpHeader.HttpStatusCode.OK, "", fileLength);
			
			// write the headers to output stream
			h.WriteHeader(processor);

			byte[] buf = new byte[8192];
			int bytesRead;
			long bytesWritten = 0;
			int offset = startOffset;
			var lol = new System.IO.StreamWriter(Console.OpenStandardOutput());
			var stream = processor.OutputStream.BaseStream;
			int sinceLastReport = 0;
			var sw = new Stopwatch();

			if (processor.HttpHeaders.ContainsKey("Range"))
			{
				string range = (string)processor.HttpHeaders["Range"];
				string start = range.Split(new char[]{'-', '='})[1];
				Console.WriteLine("[SENDFILE] Connection retried.  Resuming from {0}", start);
				fs.Seek(Convert.ToInt32(start), SeekOrigin.Begin);
				bytesWritten = fs.Position;
			}

			sw.Start();
			bool exceptionHasOccurred = false;
			while((bytesRead = fs.Read(buf, offset, 8192)) != 0 && !exceptionHasOccurred)
			{
				try
				{
					stream.Write(buf, 0, 8192);
					bytesWritten += bytesRead;
					//offset += 8192;

					if (sw.ElapsedMilliseconds > 1000)
					{
						lol.WriteLine("[SENDFILE] " + fsinfo.Name + ": [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, fileLength, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(fileLength)) * 100, (((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
						lol.Flush();
						sinceLastReport = 0;
						sw.Restart();
					}

					else sinceLastReport += bytesRead;

					if (bytesWritten == fileLength)
					{
						Console.WriteLine("[SENDFILE] " + fsinfo.Name + ": Done.");
						lol.Flush();
						break;
					}
				}
				catch (IOException e)
				{
					if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
					{
						var se = (System.Net.Sockets.SocketException)e.InnerException;
						if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
						{
							Console.WriteLine("[SENDFILE] " + "Connection was forcibly closed by the remote host");
						}
					}
					exceptionHasOccurred = true;
				}
			}
			sw.Stop();
			//_sh.writeFailure
		}
	}
}
