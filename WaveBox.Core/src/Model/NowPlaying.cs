using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model
{
	public class NowPlaying
	{
		[JsonProperty("userName")]
		public string UserName { get; set; }

		[JsonProperty("clientName")]
		public string ClientName { get; set; }

		[JsonProperty("startTime")]
		public long? StartTime { get; set; }

		[JsonProperty("endTime")]
		public long? EndTime { get; set; }

		[JsonProperty("mediaItem")]
		public IMediaItem MediaItem { get; set; }

		public NowPlaying()
		{
		}
	}
}
