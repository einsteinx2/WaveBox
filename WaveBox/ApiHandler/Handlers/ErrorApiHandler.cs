using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;
		private string _err;

		public ErrorApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
			_err = "Invalid API call";
		}

		public ErrorApiHandler(UriWrapper uriW, HttpProcessor sh, string err)
		{
			_sh = sh;
			_uriW = uriW;
			_err = err;
		}

		public void process()
		{
			Console.WriteLine("Error: " + _err);
			_sh.outputStream.Write(_err);
		}
	}
}
