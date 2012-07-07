using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaFerry.ApiHandler;
using System.Threading;
using System.Diagnostics;

namespace Bend.Util
{
	public class PmsHttpServer : HttpServer
	{
		public PmsHttpServer(int port)
			: base(port)
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			var apiHandler = ApiHandlerFactory.createRestHandler(p.http_url, p);
			apiHandler.process();
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			string data = p.http_url + "?" + inputData.ReadToEnd();
			Console.WriteLine("[HTTPSERVER] POST request: {0}", data);

			var apiHandler = ApiHandlerFactory.createRestHandler(data, p);
			apiHandler.process();
		}

		public static void sendJson(HttpProcessor _sh, string json)
		{
			_sh.writeSuccess();
			_sh.outputStream.Write(json);
		}

		public static void sendFile(HttpProcessor _sh, FileStream fs, int startOffset)
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
			PmsHttpHeader h = new PmsHttpHeader(PmsHttpHeader.HttpStatusCode.OK, "", fileLength);
			
			// write the headers to output stream
			h.writeHeader(_sh);

			byte[] buf = new byte[8192];
			int bytesRead;
			long bytesWritten = 0;
			int offset = startOffset;
			var lol = new System.IO.StreamWriter(Console.OpenStandardOutput());
			var stream = _sh.outputStream.BaseStream;
			int sinceLastReport = 0;
			var sw = new Stopwatch();

			if (_sh.httpHeaders.ContainsKey("Range"))
			{
				string range = (string)_sh.httpHeaders["Range"];
				string start = range.Split(new char[]{'-', '='})[1];
				Console.WriteLine("[SENDFILE] Connection retried.  Resuming from {0}", start);
				fs.Seek(Convert.ToInt32(start), SeekOrigin.Begin);
				bytesWritten = fs.Position;
			}

			sw.Start();
			while((bytesRead = fs.Read(buf, offset, 8192)) != 0)
			{
				stream.Write(buf, 0, 8192);
				bytesWritten += bytesRead;
				//offset += 8192;

				if (sw.ElapsedMilliseconds > 1000)
				{
					lol.WriteLine("[SENDFILE] " + fsinfo.Name + ": [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, fileLength, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(fileLength)) * 100,(((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
					lol.Flush();
					sinceLastReport = 0;
					sw.Restart();
				}

				else sinceLastReport += bytesRead;

				if(bytesWritten == fileLength)
				{
					Console.WriteLine("[SENDFILE] " + fsinfo.Name + ": Done.");
					lol.Flush();
					break;
				}
			}
			sw.Stop();
			//_sh.writeFailure
		}
	}
}
