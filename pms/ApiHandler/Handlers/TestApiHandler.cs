using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace pms.ApiHandler.Handlers
{
	class TestApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public TestApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Test: Great success!");
			_sh.outputStream.Write("<html><img src=\"http://files.sharenator.com/trollface-s800x600-183735.jpg\"></html>");
		}
	}
}
