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
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor sh)
		{
			// Turn the input string into a UriWrapper, so we can parse its components with ease
			UriWrapper uriW = new UriWrapper(uri);
			
			// authenticate before anything happens.  If no parameters are passed, or a username isn't provided,
			// or a password isn't provided, return an error handler.
			if (uriW.Parameters == null || (!uriW.Parameters.ContainsKey("u") || !uriW.Parameters.ContainsKey("p")))
				return new ErrorApiHandler(uriW, sh, "Missing authentication data");

			// Grab username and password from the URL parameters
			string username = uriW.Parameters["u"];
			string password = uriW.Parameters["p"];

			// Generate a User object given the username from the URL.  If the User is invalid, or a bad password
			// is provided then return an error handler.
			var user = new User(username);
			if (user.UserId == 0 || !user.Authenticate(password))
				return new ErrorApiHandler(uriW, sh, "Bad username or password");

			// Ensure URL contains API call
			if (uriW.FirstPart() == "api")
			{
				// Store API call type in a temporary string.  Left this assignment in case the URL format
				// changes at some point.
				string part1 = uriW.UriPart(1);

				// Determine call type.  Note that the repeated if/else is more efficient than a switch. <-- Actually switch for strings is much more efficent in Mono than .NET, so it's a wash, but in either case it's such a small portion of the total API call time as to be meaningless 
				if(part1 == "artists")
					return new ArtistsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "albums")
					return new AlbumsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "cover")
					return new CoverArtApiHandler(uriW, sh, user.UserId);
				else if(part1 == "folders")
					return new FoldersApiHandler(uriW, sh, user.UserId);
				else if(part1 == "jukebox")
					return new JukeboxApiHandler(uriW, sh, user.UserId);
                else if(part1 == "podcast")
                    return new PodcastApiHandler(uriW, sh, user.UserId);
				else if(part1 == "scrobble")
					return new ScrobbleApiHandler(uriW, sh, user.UserId);
				else if(part1 == "songs")
					return new SongsApiHandler(uriW, sh, user.UserId);
				else if(part1 == "status")
					return new StatusApiHandler(uriW, sh, user.UserId);
				else if(part1 == "stream")
					return new StreamApiHandler(uriW, sh, user.UserId);
			}

			// If the handler wasn't returned yet, return an error handler
			return new ErrorApiHandler(uriW, sh);
		}
	}
}
