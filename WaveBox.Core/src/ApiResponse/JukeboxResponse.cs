using System;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse {
    public class JukeboxResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("jukeboxStatus")]
        public JukeboxStatus JukeboxStatus { get; set; }

        [JsonPropertyName("jukeboxPlaylist")]
        public IList<IMediaItem> JukeboxPlaylist { get; set; }

        public JukeboxResponse(string error, JukeboxStatus jukeboxStatus, IList<IMediaItem> jukeboxPlaylist) {
            Error = error;
            JukeboxStatus = jukeboxStatus;
            JukeboxPlaylist = jukeboxPlaylist;
        }
    }
}

