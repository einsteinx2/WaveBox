using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.HttpServer;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public ArtistsApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Artist> listOfArtists = new List<Artist>();
			List<Song> listOfSongs = new List<Song>();
			List<Album> listOfAlbums = new List<Album>();
			string json = "";

			if (Uri.UriPart(2) == null)
			{
				listOfArtists = new Artist().AllArtists();
			}
			else
			{
				var artist = new Artist(Convert.ToInt32(Uri.UriPart(2)));
				listOfArtists.Add(artist);
				listOfSongs = artist.ListOfSongs();
				listOfAlbums = artist.ListOfAlbums();
			}

			json = JsonConvert.SerializeObject(new ArtistsResponse(null, listOfArtists, listOfAlbums, listOfSongs), Formatting.None);
			WaveBoxHttpServer.sendJson(Processor, json);
		}
	}

	class ArtistsResponse
	{
		public string Error { get; set; }
		public List<Artist> Artists { get; set; }
		public List<Album> Albums { get; set; }
		public List<Song> Songs { get; set; }

		public ArtistsResponse(string error, List<Artist> artists, List<Album> albums, List<Song> songs)
		{
			Error = error;
			Artists = artists;
			Songs = songs;
			Albums = albums;
		}
	}
}
