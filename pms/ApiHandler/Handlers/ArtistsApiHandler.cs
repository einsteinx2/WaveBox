using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public ArtistsApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			List<Artist> listOfArtists = new List<Artist>();
			string json = "";

			if (_uriW.getUriPart(2) == null)
			{
				listOfArtists = new Artist().allArtists();
			}
			else
			{
				listOfArtists.Add(new Artist(Convert.ToInt32(_uriW.getUriPart(2))));
			}

			json = JsonConvert.SerializeObject(new ArtistsResponse(null, listOfArtists), Formatting.None);
			PmsHttpServer.sendJson(_sh, json);
		}
	}

	class ArtistsResponse
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

		private List<Artist> _artists;
		public List<Artist> artists
		{
			get
			{
				return _artists;
			}
			set
			{
				_artists = value;
			}
		}

		public ArtistsResponse(string Error, List<Artist> Artists)
		{
			error = Error;
			artists = Artists;
		}
	}
}
