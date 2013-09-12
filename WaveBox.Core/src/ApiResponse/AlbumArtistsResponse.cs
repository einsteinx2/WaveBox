using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class AlbumArtistsResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("artists")]
		public IList<AlbumArtist> AlbumArtists { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("lastfmInfo")]
		public string LastfmInfo { get; set; }

		public AlbumArtistsResponse(string error, IList<AlbumArtist> albumArtists, IList<Album> albums, IList<Song> songs, string lastfmInfo = null)
		{
			Error = error;
			AlbumArtists = albumArtists;
			Songs = songs;
			Albums = albums;
			LastfmInfo = lastfmInfo;
		}
	}
}

