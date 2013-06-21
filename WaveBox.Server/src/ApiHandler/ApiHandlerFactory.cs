using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.TcpServer.Http;
using WaveBox.Core.Extensions;
using WaveBox.Core.Injected;
using Ninject;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Create an API Handler based upon source URI, or serve web interface if no API call present
		/// </summary>
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor processor)
		{
			if (logger.IsInfoEnabled) logger.Info("uri: " + uri);

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

				// Attempt to parse session ID from cookie if not already set
				if (sessionId == null)
				{
					if (processor.HttpHeaders.ContainsKey("Cookie"))
					{
						// Split each cookie into pairs
						string[] cookies = processor.HttpHeaders["Cookie"].ToString().Split(new [] {';', ',', '='}, StringSplitOptions.RemoveEmptyEntries);

						// Iterate all cookies
						for (int i = 0; i < cookies.Length; i += 2)
						{
							// Look for wavebox_session cookie
							if (cookies[i] == "wavebox_session")
							{
								sessionId = cookies[i + 1];
								logger.Info("Cookie wavebox_session: " + sessionId);
								break;
							}
						}
					}
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
					// user.SessionId will be generated on new login, so that takes precedence
					sessionId = user.SessionId == null ? sessionId : user.SessionId;
					if (sessionId != null)
					{
						// Calculate session timeout time (DateTime.Now UTC + Injection.Kernel.Get<IServerSettings>().SessionTimeout minutes)
						DateTime expire = DateTime.Now.ToUniversalTime().AddMinutes(Injection.Kernel.Get<IServerSettings>().SessionTimeout);

						// Add a delayed header so cookie will be reset on each API call (to prevent timeout)
						processor.DelayedHeaders["Set-Cookie"] = String.Format("wavebox_session={0}; Expires={1};", sessionId, expire.ToRFC1123());
					}

					// Determine call type (note: switch is actually faster than if/else for strings in Mono)
					// source: http://stackoverflow.com/questions/445067/if-vs-switch-speed
					switch (action)
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
						case "genres":
							return new GenresApiHandler(uriW, processor, user);
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
						case "playlists":
							return new PlaylistsApiHandler(uriW, processor, user);
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
				user = new User.Factory().CreateUser(username);
				if (user.UserId == null || !user.CreateSession(password, clientName))
				{
					user = null;
				}
			}
			// For the time being, our users API call does not require an individual username or password,
			// so this check will break it.  Leaving this disabled until further work is done. - mdlayher, 5/13/13
			/*
			else if (action == "users")
			{
				// Must use username and password, but don't create a session
				user = new User(username);
				if (user.UserId == null || !user.Authenticate(password))
				{
					user = null;
				}
			}
			*/
			else
			{
				// Must use sessionId
				username = User.UserNameForSessionid(sessionId);
				if ((object)username != null)
				{
					user = new User.Factory().CreateUser(username);

					// Update this user's session
					user.UpdateSession(sessionId);
				}
			}

			return user;
		}
	}
}
