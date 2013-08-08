using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.ApiHandler;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Model;
using WaveBox.Core.Extensions;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class ScrobbleApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "scrobble"; } set { } }

		/// <summary>
		/// Process a Last.fm API request
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Create Last.fm object for this user
			Lastfm lfm = new Lastfm(user);

			// Pull URL parameters for Last.fm integration
			string action = null;
			string eve = null;

			uri.Parameters.TryGetValue("action", out action);
			uri.Parameters.TryGetValue("event", out eve);

			if (action == null || action == "auth")
			{
				// If not authenticated, pass back authorization URL
				if (!lfm.SessionAuthenticated)
				{
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(null, lfm.AuthUrl)));
				}
				else
				{
					// Else, already authenticated
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMAlreadyAuthenticated")));
				}
				return;
			}

			// If Last.fm is not authenticated, provide an authorization URL
			if (!lfm.SessionAuthenticated)
			{
				logger.IfInfo("You must authenticate before you can scrobble.");

				processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNotAuthenticated", lfm.AuthUrl)));
				return;
			}

			// Create list of scrobble data
			IList<LfmScrobbleData> scrobbles = new List<LfmScrobbleData>();

			// Get Last.fm API enumerations
			LfmScrobbleType scrobbleType = Lastfm.ScrobbleTypeForString(action);

			// On invalid scrobble type, return error JSON
			if (scrobbleType == LfmScrobbleType.INVALID)
			{
				processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidScrobbleType")));
				return;
			}
			// On now playing scrobble type
			else if (scrobbleType == LfmScrobbleType.NOWPLAYING)
			{
				// Ensure ID specified for scrobble
				int id = Int32.MaxValue;
				if (!uri.Parameters.ContainsKey("id"))
				{
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoIdSpecifiedForNowPlaying")));
					return;
				}

				// Try to parse a valid ID
				bool getIdSuccess = false;
				string idTemp;
				if (!uri.Parameters.TryGetValue("id", out idTemp))
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
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidIdSpecifiedForNowPlaying")));
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
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoEventSpecifiedForScrobble")));
					return;
				}

				// Ensure input is a comma-separated pair
				string[] input = eve.Split(',');
				if ((input.Length % 2) != 0)
				{
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidInput")));
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
			dynamic resp = null;

			// If result is not null, store deserialize and store it
			if (result != null)
			{
				try
				{
					resp = JsonConvert.DeserializeObject(result);
				}
				catch (Exception e)
				{
					logger.Error(e);
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
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse()));
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				return;
			}
			// Write error JSON if it exists
			else if (resp.error != null)
			{
				try
				{
					processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(string.Format("LFM{0}: {1}", resp.error, resp.message))));
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				return;
			}
		}
	}
}
