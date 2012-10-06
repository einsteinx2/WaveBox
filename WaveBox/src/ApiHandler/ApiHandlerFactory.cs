using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.DataModel.Model;
using WaveBox.Http;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor processor)
		{
			Console.WriteLine("[ApiHandlerFactory] uri: " + uri);

			// Turn the input string into a UriWrapper, so we can parse its components with ease
			UriWrapper uriW = new UriWrapper(uri);

			// Ensure URL contains API call
			if (uriW.IsApiCall)
			{
				// Grab the action and authentication info
				string action = uriW.Action;

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
				catch {}

				User user = Authenticate(action, sessionId, username, password, clientName);
				if (user == null)
				{
					// No user object returned, so we failed to authenticate
					return new ErrorApiHandler(uriW, processor, "Authentication failed");
				}
				else
				{
					// Determine call type
					if (action == "login")
					{
						return new LoginApiHandler(uriW, processor, user);
					}
					else if (action == "artists")
					{
						return new ArtistsApiHandler(uriW, processor, user);
					}
					else if (action == "albums")
					{
						return new AlbumsApiHandler(uriW, processor, user);
					}
					else if (action == "art")
					{
						return new ArtApiHandler(uriW, processor, user);
					}
					else if (action == "folders")
					{
						return new FoldersApiHandler(uriW, processor, user);
					}
					else if (action == "jukebox")
					{
						return new JukeboxApiHandler(uriW, processor, user);
					}
					else if (action == "podcast")
					{
						return new PodcastApiHandler(uriW, processor, user);
					}
					else if (action == "scrobble")
					{
						return new ScrobbleApiHandler(uriW, processor, user);
					}
					else if (action == "songs")
					{
						return new SongsApiHandler(uriW, processor, user);
					}
					else if (action == "status")
					{
						return new StatusApiHandler(uriW, processor, user);
					}
					else if (action == "stream")
					{
						return new StreamApiHandler(uriW, processor, user);
					}
					else if (action == "transcode")
					{
						return new TranscodeApiHandler(uriW, processor, user);
					}
					else if (action == "transcodehls")
					{
						return new TranscodeHlsApiHandler(uriW, processor, user);
					}
					else if (action == "database")
					{
						return new DatabaseApiHandler(uriW, processor, user);
					}
					else if (action == "database")
					{
						return new StatsApiHandler(uriW, processor, user);
					}
				}
			}
			else
			{
				// Serve the web interface
				return new WebInterfaceHandler(uriW, processor);
			}

			// If the handler wasn't returned yet, return an error handler
			return new ErrorApiHandler(uriW, processor);
		}

		public static User Authenticate(string action, string sessionId, string username, string password, string clientName)
		{
			User user = null;
			if (action == "login" || action == "users")
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
