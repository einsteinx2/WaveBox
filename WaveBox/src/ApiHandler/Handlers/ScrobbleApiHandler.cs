using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class ScrobbleApiHandler : IApiHandler
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructor for ScrobbleApiHandler class
		/// </summary>
		public ScrobbleApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process a Last.fm API request
		/// </summary>
		public void Process()
		{
			// Create Last.fm object for this user
			Lastfm lfm = new Lastfm(User);

			// Pull URL parameters for Last.fm integration
			string action = null;
			string eve = null;
			string eveType = null;

			Uri.Parameters.TryGetValue("action", out action);
			Uri.Parameters.TryGetValue("event", out eve);
			Uri.Parameters.TryGetValue("eventType", out eveType);

			if (action == null || action == "auth")
			{
				// If not authenticated, pass back authorization URL
				if (!lfm.SessionAuthenticated)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(null, lfm.AuthUrl)));
				}
				else
				{
					// Else, already authenticated
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMAlreadyAuthenticated")));
				}
				return;
			}

			// If Last.fm is not authenticated, provide an authorization URL
			if (!lfm.SessionAuthenticated)
			{
				logger.Info("[SCROBBLE(1)] You must authenticate before you can scrobble.");

				Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNotAuthenticated", lfm.AuthUrl)));
				return;
			}

			// Create list of scrobble data
			List<LfmScrobbleData> scrobbles = new List<LfmScrobbleData>();

			// Get Last.fm API enumerations
			LfmScrobbleType scrobbleType = Lastfm.ScrobbleTypeForString(action);

			// On invalid scrobble type, return error JSON
			if (scrobbleType == LfmScrobbleType.INVALID)
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidScrobbleType")));
				return;
			}
			// On now playing scrobble type
			else if (scrobbleType == LfmScrobbleType.NOWPLAYING)
			{
				// Ensure ID specified for scrobble
				int id = Int32.MaxValue;
				if (!Uri.Parameters.ContainsKey("id"))
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoIdSpecifiedForNowPlaying")));
					return;
				}

				// Try to parse a valid ID
				bool getIdSuccess = false;
				string idTemp;
				if (!Uri.Parameters.TryGetValue("id", out idTemp))
				{
				   getIdSuccess = false;
				}
				else if (!Int32.TryParse(idTemp, out id))
				{
					getIdSuccess = false;
				}
				else
				{
					getIdSuccess = true;
				}

				// On failure, return invalid ID error
				if (!getIdSuccess)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidIdSpecifiedForNowPlaying")));
					return;
				}

				// Add successful scrobble to list, submit
				scrobbles.Add(new LfmScrobbleData(id, null));
				lfm.Scrobble(scrobbles, scrobbleType);
			}
			// Else, unknown scrobble event
			else
			{
				// On null event, return error JSON
				if (eve == null)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoEventSpecifiedForScrobble")));
					return;
				}

				// Ensure input is a comma-separated pair
				string[] input = eve.Split(',');
				if ((input.Length % 2) != 0)
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidInput")));
					return;
				}

				// Add scrobbles from input data pairs
				int i = 0;
				while (i < input.Length)
				{
					scrobbles.Add(new LfmScrobbleData(int.Parse(input[i]), long.Parse(input[i + 1])));
					i = i + 2;
				}
			}

			// Scrobble all plays
			string result = lfm.Scrobble(scrobbles, scrobbleType);
			dynamic resp;

			// If result is not null, store deserialize and store it
			if (result != null)
			{
				try
				{
					resp = JsonConvert.DeserializeObject(result);
				}
				catch (Exception e)
				{
					logger.Info("[SCROBBLE(3)] ERROR: " + e);
				}
			}
			else
			{
				return;
			}

			// Check for nowplaying or scrobbles fields
			if ((resp.nowplaying != null) || (resp.scrobbles != null))
			{
				// Write blank scrobble response
				try
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse()));
				}
				catch (Exception e)
				{
					logger.Info("[SCROBBLE(4)] ERROR: " + e);
				}
				return;
			}
			// Write error JSON if it exists
			else if (resp.error != null)
			{
				try
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(string.Format("LFM{0}: {1}", resp.error, resp.message))));
				}
				catch (Exception e)
				{
					logger.Info("[SCROBBLE(5)] ERROR: " + e);
				}
				return;
			}
		}

		private class ScrobbleResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("authUrl")]
			public string AuthUrl { get; set; }

			public ScrobbleResponse()
			{
				Error = null;
				AuthUrl = null;
			}

			public ScrobbleResponse(string error)
			{
				Error = error;
				AuthUrl = null;
			}

			public ScrobbleResponse(string error, string authUrl)
			{
				Error = error;
				AuthUrl = authUrl;
			}
		}
	}
}
