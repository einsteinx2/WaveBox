using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class SearchResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("artists")]
		public List<Artist> Artists { get; set; }

		[JsonProperty("albums")]
		public List<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public List<Song> Songs { get; set; }

		[JsonProperty("videos")]
		public List<Video> Videos { get; set; }

		public SearchResponse(string error, List<Artist> artists, List<Album> albums, List<Song> songs, List<Video> videos)
		{
			Error = error;
			Artists = artists;
			Albums = albums;
			Songs = songs;
			Videos = videos;
		}
	}
}

