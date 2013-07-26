using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core
{
	public class PlaylistsResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("playlists")]
		public List<Playlist> Playlists { get; set; }

		[JsonProperty("mediaItems")]
		public List<IMediaItem> MediaItems { get; set; }

		public PlaylistsResponse(string error, List<Playlist> playlists, List<IMediaItem> mediaItems)
		{
			Error = error;
			Playlists = playlists;
			MediaItems = mediaItems;
		}
	}
}

