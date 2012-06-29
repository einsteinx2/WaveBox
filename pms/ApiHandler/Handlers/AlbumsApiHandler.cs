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
			Console.WriteLine("Artists: Only the ALL ALBUMS call has been implemented!");

			string json = JsonConvert.SerializeObject(new AlbumsResponse(null, Album.allAlbums()), Formatting.None);
			_sh.outputStream.WriteLine(json);
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
