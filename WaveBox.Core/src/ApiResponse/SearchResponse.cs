using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class SearchResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("artists")]
		public IList<Artist> Artists { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("videos")]
		public IList<Video> Videos { get; set; }

		public SearchResponse(string error, IList<Artist> artists, IList<Album> albums, IList<Song> songs, IList<Video> videos)
		{
			Error = error;
			Artists = artists;
			Albums = albums;
			Songs = songs;
			Videos = videos;
		}
	}
}

