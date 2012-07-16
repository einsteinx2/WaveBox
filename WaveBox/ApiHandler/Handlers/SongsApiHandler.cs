using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public SongsApiHandler(UriWrapper uri, HttpProcessor processor, long userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Song> listOfSongs = new List<Song>();
			string json = "";

			if (Uri.UriPart(2) == null)
			{
				listOfSongs = Song.allSongs();
			}
			else
			{
				listOfSongs.Add(new Song(Convert.ToInt32(Uri.UriPart(2))));
			}

			json = JsonConvert.SerializeObject(new SongsResponse(null, listOfSongs), Formatting.None);
			WaveBoxHttpServer.sendJson(Processor, json);
		}
	}

	class SongsResponse
	{
		public string Error { get; set; }
		public List<Song> Songs { get; set; }

		public SongsResponse(string error, List<Song> songs)
		{
			Error = error;
			Songs = songs;
		}
	}
}
