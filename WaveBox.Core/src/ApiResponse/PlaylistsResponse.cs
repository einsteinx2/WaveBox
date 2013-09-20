using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class PlaylistsResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("playlists")]
		public IList<Playlist> Playlists { get; set; }

		[JsonProperty("mediaItems")]
		public IList<IMediaItem> MediaItems { get; set; }

		[JsonProperty("sectionPositions")]
		public PairList<string, int> SectionPositions { get; set; }

		public PlaylistsResponse(string error, IList<Playlist> playlists, IList<IMediaItem> mediaItems, PairList<string, int> sectionPositions)
		{
			Error = error;
			Playlists = playlists;
			MediaItems = mediaItems;
			SectionPositions = sectionPositions;
		}
	}
}

