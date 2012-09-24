using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }


		public AlbumsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Song> songs = new List<Song>();
			List<Album> albums = new List<Album>();

			// Try to get the album id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				Album album = new Album(id);
				albums.Add(album);
				songs = album.ListOfSongs();
			}
			else
			{
				albums = Album.AllAlbums();
			}

			try
			{
				string json = JsonConvert.SerializeObject(new AlbumsResponse(null, albums, songs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[ALBUMSAPI(1)] ERROR: " + e);
			}
		}

		private class AlbumsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("albums")]
			public List<Album> Albums { get; set; }

			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }

			public AlbumsResponse(string error, List<Album> albums, List<Song> songs)
			{
				Error = error;
				Albums = albums;
				Songs = songs;
			}
		}
	}
}
