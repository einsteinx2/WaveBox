using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Singletons;
using WaveBox.ApiHandler;
using WaveBox.Model;
using Newtonsoft.Json;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for AlbumsApiHandler
		/// </summary>
		public AlbumsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a serialized list of albums and songs in JSON format
		/// </summary>
		public void Process()
		{
			// List of songs and albums to be returned via handler
			List<Song> songs = new List<Song>();
			List<Album> albums = new List<Album>();

			// Try to get the album id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			// Return specific album on success, with its songs
			if (success)
			{
				Album album = new Album(id);
				albums.Add(album);
				songs = album.ListOfSongs();
			}
			else
			{
				// On failure, return list of all albums
				albums = Album.AllAlbums();
			}

			try
			{
				// Serialize AlbumsResponse object, write to HTTP response
				string json = JsonConvert.SerializeObject(new AlbumsResponse(null, albums, songs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
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
