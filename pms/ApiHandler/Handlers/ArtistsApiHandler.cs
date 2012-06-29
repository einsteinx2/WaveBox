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
			Console.WriteLine("Artists: Only the ALL ARTISTS call has been implemented!");
			var a = new Artist();

			string json = JsonConvert.SerializeObject(new TestResponse(null, a.allArtists()), Formatting.None);
			_sh.outputStream.WriteLine(json);
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
