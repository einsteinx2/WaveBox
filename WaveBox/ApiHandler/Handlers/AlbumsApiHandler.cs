using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private List<Song> songs;

		public AlbumsApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Album> listToReturn = new List<Album>();
			string json;

			if (Uri.UriPart(2) == null)
			{
				listToReturn = Album.AllAlbums();
			}

			else
			{
				var album = new Album(int.Parse(Uri.UriPart(2)));
				listToReturn.Add(album);
				songs = album.ListOfSongs();
			}

			json = JsonConvert.SerializeObject(new AlbumsResponse(null, listToReturn, songs), Formatting.None);

			WaveBoxHttpServer.sendJson(Processor, json);
		}
	}

	class AlbumsResponse
	{
		public string Error { get; set; }
		public List<Album> Albums { get; set; }
		public List<Song> Songs { get; set; }

		public AlbumsResponse(string error, List<Album> albums, List<Song> songs)
		{
			Error = error;
			Albums = albums;
			Songs = songs;
		}
	}
}
