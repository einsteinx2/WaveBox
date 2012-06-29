using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class CoverArtApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public CoverArtApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{


			Console.WriteLine("CoverArt: Not implemented yet.");
			_sh.outputStream.Write("CoverArt: Not implemented yet.");
		}
	}
}
