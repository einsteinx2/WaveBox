using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.ApiHandler;
using MediaFerry.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public AlbumsApiHandler(UriWrapper uriW, HttpProcessor sh)
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

			else listToReturn.Add(new Album(int.Parse(_uriW.getUriPart(2))));

			json = JsonConvert.SerializeObject(new AlbumsResponse(null, listToReturn), Formatting.None);


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

		public AlbumsResponse(string Error, List<Album> Albums)
		{
			error = Error;
			albums = Albums;
		}
	}
}
