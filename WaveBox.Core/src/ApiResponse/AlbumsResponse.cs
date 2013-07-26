using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class AlbumsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		public AlbumsResponse(string error, IList<Album> albums, IList<Song> songs)
		{
			Error = error;
			Albums = albums;
			Songs = songs;
		}
	}
}

