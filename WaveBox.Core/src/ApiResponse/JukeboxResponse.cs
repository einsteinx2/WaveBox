using System;
using Newtonsoft.Json;
using WaveBox.Core.Model;
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
		public IList<IMediaItem> JukeboxPlaylist { get; set; }

		public JukeboxResponse(string error, JukeboxStatus jukeboxStatus, IList<IMediaItem> jukeboxPlaylist)
		{
			Error = error;
			JukeboxStatus = jukeboxStatus;
			JukeboxPlaylist = jukeboxPlaylist;
		}
	}
}

