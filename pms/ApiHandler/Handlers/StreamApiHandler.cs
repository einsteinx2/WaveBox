using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class StreamApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public StreamApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Stream: Not implemented yet.");
			_sh.outputStream.Write("Stream: Not implemented yet.");
		}
	}
}
