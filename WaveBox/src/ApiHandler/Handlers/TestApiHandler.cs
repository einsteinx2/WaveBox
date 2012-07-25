using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.HttpServer;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class TestApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public TestApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			Console.WriteLine("[TESTAPI] Great success!");

			var a = new Artist();

			string json = JsonConvert.SerializeObject(new TestResponse(null, a.AllArtists()), Formatting.None);
			Processor.OutputStream.WriteLine(json);

			//foreach (var g in a.allArtists())
			//{
			//    _sh.outputStream.Write(JsonConvert.SerializeObject(g, Formatting.None) + ",");
			//    //Console.WriteLine(g.ArtistName + " " + g.ArtistId);
			//}
		}
	}

	class TestResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("artists")]
		public List<Artist> Artists { get; set; }

		public TestResponse(string error, List<Artist> artists)
		{
			Error = error;
			Artists = artists;
		}
	}
}
