using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using WaveBox.DataModel.Model;
using Bend.Util;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor sh)
		{
			UriWrapper uriW = new UriWrapper(uri);
			
			// authenticate before anything happens.
			int userId;
			if (uriW.Parameters == null || (!uriW.Parameters.ContainsKey("u") || !uriW.Parameters.ContainsKey("p")))
			{
				return new ErrorApiHandler(uriW, sh, "Missing authentication data");
			}

			string username, password;
			uriW.Parameters.TryGetValue("u", out username);
			uriW.Parameters.TryGetValue("p", out password);

			var user = new User(username);
			if (user.UserId == 0 || !user.Authenticate(password))
			{
				return new ErrorApiHandler(uriW, sh, "Bad username or password");
			}

			userId = user.UserId;

			IApiHandler returnHandler = null;

			if (uriW.FirstPart() == "api")
			{
				string part1 = uriW.UriPart(1);

				if (part1 == "test")
				{
					returnHandler = new TestApiHandler(uriW, sh, userId);
				}

				else if (part1 == "folders")
				{
					returnHandler = new FoldersApiHandler(uriW, sh, userId);
				}

				else if (part1 == "jukebox")
				{
					returnHandler = new JukeboxApiHandler(uriW, sh, userId);
				}

				else if (part1 == "artists")
				{
					returnHandler = new ArtistsApiHandler(uriW, sh, userId);
				}

				else if (part1 == "albums")
				{
					returnHandler = new AlbumsApiHandler(uriW, sh, userId);
				}

				else if (part1 == "songs")
				{
					returnHandler = new SongsApiHandler(uriW, sh, userId);
				}

				else if (part1 == "stream")
				{
					returnHandler = new StreamApiHandler(uriW, sh, userId);
				}

				else if (part1 == "cover")
				{
					returnHandler = new CoverArtApiHandler(uriW, sh, userId);
				}

				else if (part1 == "status")
				{
					returnHandler = new StatusApiHandler(uriW, sh, userId);
				}
			}

			if (returnHandler == null)
			{
				returnHandler = new ErrorApiHandler(uriW, sh);
			}

			return returnHandler;
		}
	}
}
