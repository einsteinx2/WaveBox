using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public SongsApiHandler(UriWrapper uri, IHttpProcessor processor, int userId)
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

			try
			{
				json = JsonConvert.SerializeObject(new SongsResponse(null, listOfSongs), Formatting.None);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[SONGAPI] ERROR: " + e.ToString());
			}
		}
	}

	class SongsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("songs")]
		public List<Song> Songs { get; set; }

		public SongsResponse(string error, List<Song> songs)
		{
			Error = error;
			Songs = songs;
		}
	}
}
