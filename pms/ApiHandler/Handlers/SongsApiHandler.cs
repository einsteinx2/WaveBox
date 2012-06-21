using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace pms.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public SongsApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Songs: Not implemented yet.");
			_sh.outputStream.Write("Songs: Not implemented yet.");
		}
	}
}
