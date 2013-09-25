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

		[JsonProperty("albumArtists")]
		public IList<AlbumArtist> AlbumArtists { get; set; }

		[JsonProperty("albums")]
		public IList<Album> Albums { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("counts")]
		public Dictionary<string, int> Counts { get; set; }

		[JsonProperty("lastfmInfo")]
		public string LastfmInfo { get; set; }

		[JsonProperty("sectionPositions")]
		public PairList<string, int> SectionPositions { get; set; }

		public AlbumArtistsResponse(string error, IList<AlbumArtist> albumArtists, IList<Album> albums, IList<Song> songs, Dictionary<string, int> counts, string lastfmInfo, PairList<string, int> sectionPositions)
		{
			Error = error;
			AlbumArtists = albumArtists;
			Songs = songs;
			Albums = albums;
			Counts = counts;
			LastfmInfo = lastfmInfo;
			SectionPositions = sectionPositions;
		}
	}
}

