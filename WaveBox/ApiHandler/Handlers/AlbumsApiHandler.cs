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
		private HttpProcessor _sh;
		private UriWrapper _uriW;
		private List<Song> songs;

		public AlbumsApiHandler(UriWrapper uriW, HttpProcessor sh, int userId)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			List<Album> listToReturn = new List<Album>();
			string json;

			if (_uriW.getUriPart(2) == null)
			{
				listToReturn = Album.allAlbums();
			}

			else
			{
				var album = new Album(int.Parse(_uriW.getUriPart(2)));
				listToReturn.Add(album);
				songs = album.listOfSongs();
			}

			json = JsonConvert.SerializeObject(new AlbumsResponse(null, listToReturn, songs), Formatting.None);


			PmsHttpServer.sendJson(_sh, json);
		}
	}

	class AlbumsResponse
	{
		private string _error;
		public string error
		{
			get
			{
				return _error;
			}
			set
			{
				_error = value;
			}
		}

		private List<Album> _albums;
		public List<Album> albums
		{
			get
			{
				return _albums;
			}
			set
			{
				_albums = value;
			}
		}

		private List<Song> _songs;
		public List<Song> songs
		{
			get
			{
				return _songs;
			}
			set
			{
				_songs = value;
			}
		}

		public AlbumsResponse(string Error, List<Album> Albums, List<Song> Songs)
		{
			error = Error;
			albums = Albums;
			songs = Songs;
		}
	}
}
