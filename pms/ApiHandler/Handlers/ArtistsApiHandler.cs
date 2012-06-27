using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public ArtistsApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Artists: Not implemented yet.");
			_sh.outputStream.Write("Artists: Not implemented yet.");
		}
	}
}
