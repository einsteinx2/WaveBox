using System;
using Newtonsoft.Json;

namespace WaveBox.Core.ApiResponse
{
	public class JukeboxStatus
	{
		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("currentIndex")]
		public int CurrentIndex { get; set; }

		[JsonProperty("progress")]
		public double Progress { get; set; }

		public JukeboxStatus(string state, int currentIndex, double progress)
		{
			State = state;
			CurrentIndex = currentIndex;
			Progress = progress;
		}
	}
}

