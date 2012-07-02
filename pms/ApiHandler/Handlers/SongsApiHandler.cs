using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public SongsApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			List<Song> listOfSongs = new List<Song>(); Song.allSongs();
			string json = "";

			if (_uriW.getUriPart(2) == null)
			{
				listOfSongs = Song.allSongs();
			}
			else
			{
				listOfSongs.Add(new Song(Convert.ToInt32(_uriW.getUriPart(2))));
			}

			json = JsonConvert.SerializeObject(new SongsResponse(null, listOfSongs), Formatting.None);
			PmsHttpServer.sendJson(_sh, json);
		}
	}

	class SongsResponse
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

		public SongsResponse(string Error, List<Song> Songs)
		{
			error = Error;
			songs = Songs;
		}
	}
}
