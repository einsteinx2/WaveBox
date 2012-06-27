using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;
using MediaFerry.DataModel.Model;

namespace MediaFerry.ApiHandler.Handlers
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

			var a = new Artist();

			foreach (var g in a.allArtists())
			{
				Console.WriteLine(g.ArtistName + " " + g.ArtistId);
			}
		}
	}
}
