using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model {
    public class NowPlaying {
        [JsonPropertyName("startTime")]
        public long? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public long? EndTime { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("mediaItem")]
        public IMediaItem MediaItem { get; set; }

        [JsonIgnore]
        public Timer Timer { get; set; }

        public NowPlaying() {
        }

        public override string ToString() {
            return String.Format("[NowPlaying: StartTime={0}, EndTime={1}, User={2}]", this.StartTime, this.EndTime, this.User.UserName);
        }
    }
}
