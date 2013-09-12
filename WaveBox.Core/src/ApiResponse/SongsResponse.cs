using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class SongsResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		public SongsResponse(string error, IList<Song> songs)
		{
			Error = error;
			Songs = songs;
		}
	}
}

