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
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public StreamApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			if (Uri.UriPart(2) != null)
			{
				try
				{
					// this will really be a mediaitem object, but it's just a song for now.
					var song = new Song(Convert.ToInt32(Uri.UriPart(2)));
					var fs = song.File();
					WaveBoxHttpServer.sendFile(Processor, fs, 0);
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
