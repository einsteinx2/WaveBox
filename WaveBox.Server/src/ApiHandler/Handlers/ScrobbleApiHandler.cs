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

		public string Name { get { return "scrobble"; } }

		// Standard users may scrobble
		public bool CheckPermission(User user, string action)
		{
			return user.HasPermission(Role.User);
		}

		/// <summary>
		/// Process a Last.fm API request
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Create Last.fm object for this user
			Lastfm lfm = new Lastfm(user);

			// Pull URL parameters for Last.fm integration
			string eve = null;
			uri.Parameters.TryGetValue("event", out eve);

			if (uri.Action == null || uri.Action == "auth")
			{
				// If not authenticated, pass back authorization URL
				if (!lfm.SessionAuthenticated)
				{
					processor.WriteJson(new ScrobbleResponse(null, lfm.AuthUrl));
				}
				else
				{
					// Else, already authenticated
					processor.WriteJson(new ScrobbleResponse("LFMAlreadyAuthenticated"));
				}
				return;
			}

			// If Last.fm is not authenticated, provide an authorization URL
			if (!lfm.SessionAuthenticated)
			{
				logger.IfInfo("You must authenticate before you can scrobble.");

				processor.WriteJson(new ScrobbleResponse("LFMNotAuthenticated", lfm.AuthUrl));
				return;
			}

			// Create list of scrobble data
			IList<LfmScrobbleData> scrobbles = new List<LfmScrobbleData>();

			// Get Last.fm API enumerations
			LfmScrobbleType scrobbleType = Lastfm.ScrobbleTypeForString(uri.Action);

			// On invalid scrobble type, return error JSON
			if (scrobbleType == LfmScrobbleType.INVALID)
			{
				processor.WriteJson(new ScrobbleResponse("LFMInvalidScrobbleType"));
				return;
			}

			// On now playing scrobble type
			if (scrobbleType == LfmScrobbleType.NOWPLAYING)
			{
				// Ensure ID specified for scrobble
				if (uri.Id == null)
				{
					processor.WriteJson(new ScrobbleResponse("LFMNoIdSpecifiedForNowPlaying"));
					return;
				}

				// Add successful scrobble to list, submit
				scrobbles.Add(new LfmScrobbleData((int)uri.Id, null));
				lfm.Scrobble(scrobbles, scrobbleType);
			}
			// Else, unknown scrobble event
			else
			{
				// On null event, return error JSON
				if (eve == null)
				{
					processor.WriteJson(new ScrobbleResponse("LFMNoEventSpecifiedForScrobble"));
					return;
				}

				// Ensure input is a comma-separated pair
				string[] input = eve.Split(',');
				if ((input.Length % 2) != 0)
				{
					processor.WriteJson(new ScrobbleResponse("LFMInvalidInput"));
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

			// No response, service must be offline
			if (result == null)
			{
				processor.WriteJson(new ScrobbleResponse("LFMServiceOffline"));
				return;
			}

			// If result is not null, store deserialize and store it
			try
			{
				resp = JsonConvert.DeserializeObject(result);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			// Check for nowplaying or scrobbles fields
			if ((resp.nowplaying != null) || (resp.scrobbles != null))
			{
				// Write blank scrobble response
				processor.WriteJson(new ScrobbleResponse());
				return;
			}
			// Write error JSON if it exists
			else if (resp.error != null)
			{
				processor.WriteJson(new ScrobbleResponse(string.Format("LFM{0}: {1}", resp.error, resp.message)));
				return;
			}
		}
	}
}
