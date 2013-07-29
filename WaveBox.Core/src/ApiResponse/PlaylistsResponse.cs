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
		public IList<Playlist> Playlists { get; set; }

		[JsonProperty("mediaItems")]
		public IList<IMediaItem> MediaItems { get; set; }

		public PlaylistsResponse(string error, IList<Playlist> playlists, IList<IMediaItem> mediaItems)
		{
			Error = error;
			Playlists = playlists;
			MediaItems = mediaItems;
		}
	}
}

