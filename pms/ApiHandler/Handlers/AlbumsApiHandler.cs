using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pms.ApiHandler;
using Bend.Util;

namespace pms.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public AlbumsApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Albums: Not implemented yet.");
			_sh.outputStream.Write("Albums: Not implemented yet.");
		}
	}
}
