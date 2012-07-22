using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.DataModel.Model;
using WaveBox.HttpServer;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor sh)
		{
			// Turn the input string into a UriWrapper, so we can parse its components with ease
			UriWrapper uriW = new UriWrapper(uri);
			
			// authenticate before anything happens.  If no parameters are passed, or a username isn't provided,
			// or a password isn't provided, return an error.
			if (uriW.Parameters == null || (!uriW.Parameters.ContainsKey("u") || !uriW.Parameters.ContainsKey("p")))
				return new ErrorApiHandler(uriW, sh, "[ERROR] Missing authentication data");

			// Grab username and password from the URL parameters
			string username, password;
			uriW.Parameters.TryGetValue("u", out username);
			uriW.Parameters.TryGetValue("p", out password);

			// Generate a User object given the username from the URL.  If the User is invalid, or a bad password
			// is provided
			var user = new User(username);
			if (user.UserId == 0 || !user.Authenticate(password))
				return new ErrorApiHandler(uriW, sh, "[ERROR] Bad username or password");

			// Create generic return handler, to be replaced by another handler below
			IApiHandler returnHandler = null;

			// Ensure URL contains API call
			if (uriW.FirstPart() == "api")
			{
				// Store API call type in a temporary string.  Left this assignment in case the URL format
				// changes at some point.
				string part1 = uriW.UriPart(1);

				// Determine call type.  Note that the repeated if/else is more efficient than a switch.
				if(part1 == "artists")
					returnHandler = new ArtistsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "albums")
					returnHandler = new AlbumsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "cover")
					returnHandler = new CoverArtApiHandler(uriW, sh, user.UserId);
				else if(part1 == "folders")
					returnHandler = new FoldersApiHandler(uriW, sh, user.UserId);
				else if(part1 == "jukebox")
					returnHandler = new JukeboxApiHandler(uriW, sh, user.UserId);
				else if(part1 == "scrobble")
					returnHandler = new ScrobbleApiHandler(uriW, sh, user.UserId);
				else if(part1 == "songs")
					returnHandler = new SongsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "status")
					returnHandler = new StatusApiHandler(uriW, sh, user.UserId);
				else if(part1 == "stream")
					returnHandler = new StreamApiHandler(uriW, sh, user.UserId);
				else if(part1 == "test")
					returnHandler = new TestApiHandler(uriW, sh, user.UserId);
			}
			// If the return handler was never set, set an ErrorApiHandler into it
			if (returnHandler == null)
				returnHandler = new ErrorApiHandler(uriW, sh);

			// Return the ApiHandler
			return returnHandler;
		}
	}
}
