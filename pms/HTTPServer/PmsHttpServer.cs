using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaFerry.ApiHandler;
using System.Threading;

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
			Console.WriteLine("POST request: {0}", data);

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
				Console.WriteLine(e.ToString());
			}

			// new http header object
			PmsHttpHeader h = new PmsHttpHeader(PmsHttpHeader.HttpStatusCode.OK, "", fileLength);
			
			// write the headers to output stream
			h.writeHeader(_sh);

			byte[] buf = new byte[8192];
			int bytesRead;
			int bytesWritten = 0;
			int offset = startOffset;
			var lol = new System.IO.StreamWriter(Console.OpenStandardOutput());
			var stream = _sh.outputStream.BaseStream;

			while((bytesRead = fs.Read(buf, offset, 8192)) != 0)
			{
				stream.Write(buf, 0, 8192);
				bytesWritten += bytesRead;
				//offset += 8192;
				lol.WriteLine(fsinfo.Name + ": [ {0} / {1} ] written to output stream", bytesWritten, fileLength, fs.Position);
				lol.Flush();

				if(bytesWritten == fileLength)
				{
					Console.WriteLine("reached eof.  breaking.");
					lol.Flush();
					break;
				}
			}
			//_sh.writeFailure
		}
	}
}
