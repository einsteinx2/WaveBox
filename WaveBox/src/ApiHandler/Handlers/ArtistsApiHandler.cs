using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public ArtistsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Artist> listOfArtists = new List<Artist>();
			List<Song> listOfSongs = new List<Song>();
			List<Album> listOfAlbums = new List<Album>();

			// Try to get the artist id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				Artist artist = new Artist(id);
				listOfArtists.Add(artist);
				listOfAlbums = artist.ListOfAlbums();

				string includeSongs;
				Uri.Parameters.TryGetValue("includeSongs", out includeSongs);

				if ((object)includeSongs != null && includeSongs.ToLower() == "true")
				{
					listOfSongs = artist.ListOfSongs();
				}
			}
			else
			{
				listOfArtists = new Artist().AllArtists();
			}

			try
			{
				string json = JsonConvert.SerializeObject(new ArtistsResponse(null, listOfArtists, listOfAlbums, listOfSongs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[ARTISTSAPI(1)] ERROR: " + e.ToString());
			}
		}

		private class ArtistsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("artists")]
			public List<Artist> Artists { get; set; }

			[JsonProperty("albums")]
			public List<Album> Albums { get; set; }

			[JsonProperty("songs")]
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
}
