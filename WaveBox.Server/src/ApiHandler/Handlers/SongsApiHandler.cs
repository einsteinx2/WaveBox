using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for SongsApiHandler
		/// </summary>
		public SongsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process generates a JSON list of songs
		/// </summary>
		public void Process()
		{
			// Return list of songs
			IList<Song> listOfSongs = new List<Song>();

			// Fetch song ID from parameters
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				// Add song by ID to the list
				listOfSongs.Add(new Song.Factory().CreateSong(id));
			}
			else
			{
				// Add all songs to list
				listOfSongs = Song.AllSongs();
			}

			// Return generated list of songs
			try
			{
				string json = JsonConvert.SerializeObject(new SongsResponse(null, listOfSongs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		private class SongsResponse
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
}
