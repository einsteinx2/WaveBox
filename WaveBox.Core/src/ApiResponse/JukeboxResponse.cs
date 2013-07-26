using System;
using Newtonsoft.Json;
using WaveBox.Model;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse
{
	public class JukeboxResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("jukeboxStatus")]
		public JukeboxStatus JukeboxStatus { get; set; }

		[JsonProperty("jukeboxPlaylist")]
		public List<IMediaItem> JukeboxPlaylist { get; set; }

		public JukeboxResponse(string error, JukeboxStatus jukeboxStatus, List<IMediaItem> jukeboxPlaylist)
		{
			Error = error;
			JukeboxStatus = jukeboxStatus;
			JukeboxPlaylist = jukeboxPlaylist;
		}
	}
}

