using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Core.ApiResponse
{
	public class GenresResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("genres")]
		public List<Genre> Genres { get; set; }

		[JsonProperty("folders")]
		public List<Folder> Folders { get; set; }

		[JsonProperty("artists")]
		public List<Artist> Artists { get; set; }

		[JsonProperty("albums")]
		public List<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public List<Song> Songs { get; set; }

		public GenresResponse(string error, List<Genre> genres, List<Folder> folders, List<Artist> artists, List<Album> albums, List<Song> songs)
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

