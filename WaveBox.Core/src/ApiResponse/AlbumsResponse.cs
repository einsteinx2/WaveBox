using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class AlbumsResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("sectionPositions")]
		public PairList<string, int> SectionPositions { get; set; }

		public AlbumsResponse(string error, IList<Album> albums, IList<Song> songs, PairList<string, int> sectionPositions)
		{
			Error = error;
			Albums = albums;
			Songs = songs;
			SectionPositions = sectionPositions;
		}
	}
}

