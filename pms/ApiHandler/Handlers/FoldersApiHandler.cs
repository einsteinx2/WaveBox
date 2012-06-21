using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace pms.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public FoldersApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Folders: Not implemented yet.");
			_sh.outputStream.Write("Folders: Not implemented yet.");
		}
	}
}
