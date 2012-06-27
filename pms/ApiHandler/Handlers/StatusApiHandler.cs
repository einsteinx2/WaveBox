using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public StatusApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Status: Not implemented yet.");
			_sh.outputStream.Write("Status: Not implemented yet.");
		}
	}
}
