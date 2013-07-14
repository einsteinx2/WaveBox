using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class SongsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "songs"; } set { } }

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructor for SongsApiHandler
		/// </summary>
		public SongsApiHandler()
		{
		}

		/// <summary>
		/// Prepare parameters via factory
		/// </summary>
		public void Prepare(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process generates a JSON list of songs
		/// </summary>
		public void Process()
		{
			// Return list of songs
			IList<Song> listOfSongs = new List<Song>();

			// Fetch song ID from parameters
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				if (!Int32.TryParse(Uri.Parameters["id"], out id))
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'id' requires a valid integer", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Add song by ID to the list
				listOfSongs.Add(new Song.Factory().CreateSong(id));
			}
			// Check for a request for range of songs
			else if (Uri.Parameters.ContainsKey("range"))
			{
				string[] range = Uri.Parameters["range"].Split(',');

				// Ensure valid range was parsed
				if (range.Length != 2)
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'range' requires a valid, comma-separated character tuple", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Validate as characters
				char start, end;
				if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end))
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'range' requires characters which are single alphanumeric values", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Grab range of songs
				listOfSongs = Song.RangeSongs(start, end);
			}

			// Check for a request to limit/paginate songs, like SQL
			// Note: can be combined with range or all songs
			if (Uri.Parameters.ContainsKey("limit") && !Uri.Parameters.ContainsKey("id"))
			{
				string[] limit = Uri.Parameters["limit"].Split(',');

				// Ensure valid limit was parsed
				if (limit.Length < 1 || limit.Length > 2 )
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Validate as integers
				int index = 0;
				int duration = Int32.MinValue;
				if (!Int32.TryParse(limit[0], out index))
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'limit' requires a valid integer start index", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Ensure positive index
				if (index < 0)
				{
					string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'limit' requires a non-negative integer start index", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Check for duration
				if (limit.Length == 2)
				{
					if (!Int32.TryParse(limit[1], out duration))
					{
						string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'limit' requires a valid integer duration", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						Processor.WriteJson(json);
						return;
					}

					// Ensure positive duration
					if (duration < 0)
					{
						string json = JsonConvert.SerializeObject(new SongsResponse("Parameter 'limit' requires a non-negative integer duration", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						Processor.WriteJson(json);
						return;
					}
				}

				// Check if results list already populated by range
				if (listOfSongs.Count > 0)
				{
					// No duration?  Return just specified number of songs
					if (duration == Int32.MinValue)
					{
						listOfSongs = listOfSongs.Skip(0).Take(index).ToList();
					}
					else
					{
						// Else, return songs starting at index, up to count duration
						listOfSongs = listOfSongs.Skip(index).Take(duration).ToList();
					}
				}
				else
				{
					// If no songs in list, grab directly using model method
					listOfSongs = Song.LimitSongs(index, duration);
				}
			}

			// Finally, if no songs already in list, send the whole list
			if (listOfSongs.Count == 0)
			{
				listOfSongs = Song.AllSongs();
			}

			try
			{
				// Send it!
				string json = JsonConvert.SerializeObject(new SongsResponse(null, listOfSongs), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
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
