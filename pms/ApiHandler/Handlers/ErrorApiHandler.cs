using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public ErrorApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Error: Not implemented yet.");
			_sh.outputStream.Write("Error: Not implemented yet.");
		}
	}
}
