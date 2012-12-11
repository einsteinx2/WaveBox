using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.DataModel.Model;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Create an API Handler based upon source URI, or serve web interface if no API call present
		/// </summary>
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor processor)
		{
			logger.Info("[ApiHandlerFactory] uri: " + uri);

			// Turn the input string into a UriWrapper, so we can parse its components with ease
			UriWrapper uriW = new UriWrapper(uri);

			// Ensure URL contains API call
			if (uriW.IsApiCall)
			{
				// Grab the action and authentication info (if call is /api/, no action, so return error)
				string action = "";
				try
				{
					action = uriW.Action;
				}
				catch
				{
					action = null;
					return new ErrorApiHandler(uriW, processor, "No API call action provided");
				}

				string sessionId = null;
				string username = null;
				string password = null;
				string clientName = null;

				try
				{
					uriW.Parameters.TryGetValue("s", out sessionId);
					uriW.Parameters.TryGetValue("u", out username);
					uriW.Parameters.TryGetValue("p", out password);
					uriW.Parameters.TryGetValue("c", out clientName);
				}
				catch
				{
					// If we fail to grab a parameter needed by a requested handler, it will report the error
				}

				// Authenticate user
				User user = Authenticate(action, sessionId, username, password, clientName);
				if (user == null)
				{
					// No user object returned, so we failed to authenticate
					return new ErrorApiHandler(uriW, processor, "Authentication failed");
				}
				else
				{
					// Determine call type (note: switch is actually faster than if/else for strings in Mono)
					// source: http://stackoverflow.com/questions/445067/if-vs-switch-speed
					switch(action)
					{
						case "albums":
							return new AlbumsApiHandler(uriW, processor, user);
						case "art":
							return new ArtApiHandler(uriW, processor, user);
						case "artists":
							return new ArtistsApiHandler(uriW, processor, user);
						case "database":
							return new DatabaseApiHandler(uriW, processor, user);
						case "folders":
							return new FoldersApiHandler(uriW, processor, user);
						case "jukebox":
							return new JukeboxApiHandler(uriW, processor, user);
						case "login":
							return new LoginApiHandler(uriW, processor, user);
						case "podcast":
							return new PodcastApiHandler(uriW, processor, user);
						case "search":
							return new SearchApiHandler(uriW, processor, user);
						case "scrobble":
							return new ScrobbleApiHandler(uriW, processor, user);
						case "settings":
							return new SettingsApiHandler(uriW, processor, user);
						case "songs":
							return new SongsApiHandler(uriW, processor, user);
						case "status":
							return new StatusApiHandler(uriW, processor, user);
						case "stats":
							return new StatsApiHandler(uriW, processor, user);
						case "stream":
							return new StreamApiHandler(uriW, processor, user);
						case "transcode":
							return new TranscodeApiHandler(uriW, processor, user);
						case "transcodehls":
							return new TranscodeHlsApiHandler(uriW, processor, user);
						case "users":
							return new UsersApiHandler(uriW, processor, user);
						case "videos":
							return new VideosApiHandler(uriW, processor, user);
						default:
							return new ErrorApiHandler(uriW, processor);
					}
				}
			}
			else
			{
				// Else, no API call present, so serve the web interface
				return new WebInterfaceHandler(uriW, processor);
			}
		}

		/// <summary>
		/// Authenticate a user or session against the database
		/// </summary>
		public static User Authenticate(string action, string sessionId, string username, string password, string clientName)
		{
			User user = null;
			if (action == "login")
			{
				// Must use username and password, and create a session
				user = new User(username);
				if (user.UserId == null || !user.CreateSession(password, clientName))
				{
					user = null;
				}
			}
			else if (action == "users")
			{
				// Must use username and password, but don't create a session
				user = new User(username);
				if (user.UserId == null || !user.Authenticate(password))
				{
					user = null;
				}
			}
			else
			{
				// Must use sessionId
				username = User.UserNameForSessionid(sessionId);
				if ((object)username != null)
				{
					user = new User(username);
				}
			}

			return user;
		}
	}
}
