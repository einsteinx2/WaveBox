using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Core.ApiResponse
{
	public class ArtistsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("artists")]
		public IList<Artist> Artists { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("lastfmInfo")]
		public string LastfmInfo { get; set; }

		public ArtistsResponse(string error, IList<Artist> artists, IList<Album> albums, IList<Song> songs, string lastfmInfo = null)
		{
			Error = error;
			Artists = artists;
			Songs = songs;
			Albums = albums;
			LastfmInfo = lastfmInfo;
		}
	}
}

