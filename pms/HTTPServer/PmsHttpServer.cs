using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using pms.ApiHandler;

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
			Console.WriteLine("POST request: {0}", p.http_url);
			string data = inputData.ReadToEnd();

			p.outputStream.WriteLine("<html><body><h1>test server</h1>");
			p.outputStream.WriteLine("<a href=/test>return</a><p>");
			p.outputStream.WriteLine("postbody: <pre>{0}</pre>", data);
		}
	}
}
