using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model
{
	public class NowPlaying
	{
		[JsonProperty("startTime")]
		public long? StartTime { get; set; }

		[JsonProperty("endTime")]
		public long? EndTime { get; set; }

		[JsonProperty("user")]
		public User User { get; set; }

		[JsonProperty("mediaItem")]
		public IMediaItem MediaItem { get; set; }

		[JsonIgnore]
		public Timer Timer { get; set; }

		public NowPlaying()
		{
		}

		public override string ToString()
		{
			return String.Format("[NowPlaying: StartTime={0}, EndTime={1}, User={2}]", this.StartTime, this.EndTime, this.User.UserName);
		}
	}
}
