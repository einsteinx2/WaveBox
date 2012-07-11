using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using System.IO;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
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
			if (_uriW.getUriPart(2) != null)
			{
				try
				{
					// this will really be a mediaitem object, but it's just a song for now.
					var song = new Song(Convert.ToInt32(_uriW.getUriPart(2)));
					var fs = song.file();
					PmsHttpServer.sendFile(_sh, fs, 0);
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
