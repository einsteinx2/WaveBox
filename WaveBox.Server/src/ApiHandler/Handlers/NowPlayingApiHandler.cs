using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class NowPlayingApiHandler : IApiHandler
	{
		public string Name { get { return "nowplaying"; } set { } }

		/// <summary>
		/// Process returns a readonly list of now playing media items, filtered by optional parameters
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Get NowPlayingService instance
			NowPlayingService nowPlayingService = (NowPlayingService)ServiceManager.GetInstance("nowplaying");

			// Ensure service is running
			if (nowPlayingService == null)
			{
				processor.WriteJson(new NowPlayingResponse("NowPlayingService is not running!", null));
				return;
			}

			// Store list of now playing objects
			IList<NowPlaying> nowPlaying = nowPlayingService.Playing;

			// Filter by user name
			if (uri.Parameters.ContainsKey("user"))
			{
				nowPlaying = nowPlaying.Where(x => x.UserName == uri.Parameters["user"]).ToList();
			}

			// Filter by client name
			if (uri.Parameters.ContainsKey("client"))
			{
				nowPlaying = nowPlaying.Where(x => x.ClientName == uri.Parameters["client"]).ToList();
			}

			// Return list of now playing items
			processor.WriteJson(new NowPlayingResponse(null, nowPlaying));
			return;
		}
	}
}
