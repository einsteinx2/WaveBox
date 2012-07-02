using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaFerry.ApiHandler;

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
			_sh.httpHeaders.Remove("Content-Type");
			_sh.httpHeaders.Add("Content-Type", "application/json; charset=UTF-8");

			_sh.outputStream.Write(json);
		}
	}
}
