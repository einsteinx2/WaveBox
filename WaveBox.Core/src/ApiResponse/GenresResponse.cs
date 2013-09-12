using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class GenresResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("genres")]
		public IList<Genre> Genres { get; set; }

		[JsonProperty("folders")]
		public IList<Folder> Folders { get; set; }

		[JsonProperty("artists")]
		public IList<Artist> Artists { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		public GenresResponse(string error, IList<Genre> genres, IList<Folder> folders, IList<Artist> artists, IList<Album> albums, IList<Song> songs)
		{
			Error = error;
			Genres = genres;
			Folders = folders;
			Artists = artists;
			Albums = albums;
			Songs = songs;
		}
	}
}

