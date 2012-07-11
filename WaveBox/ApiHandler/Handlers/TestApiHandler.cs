using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class TestApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public TestApiHandler(UriWrapper uriW, HttpProcessor sh, int userId)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Test: Great success!");

			var a = new Artist();

			string json = JsonConvert.SerializeObject(new TestResponse(null, a.allArtists()), Formatting.None);
			_sh.outputStream.WriteLine(json);

			//foreach (var g in a.allArtists())
			//{
			//    _sh.outputStream.Write(JsonConvert.SerializeObject(g, Formatting.None) + ",");
			//    //Console.WriteLine(g.ArtistName + " " + g.ArtistId);
			//}
		}
	}

	class TestResponse
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

		public TestResponse(string Error, List<Artist> Artists)
		{
			error = Error;
			artists = Artists;
		}
	}
}
