using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler.Handlers;
using Bend.Util;

namespace WaveBox.ApiHandler
{
	class ApiHandlerFactory
	{
		public static IApiHandler createRestHandler(string uri, HttpProcessor sh)
		{
			IApiHandler returnHandler = null;
			UriWrapper uriW = new UriWrapper(uri);

			if (uriW.getFirstPart() == "api")
			{
				string part1 = uriW.getUriPart(1);

				if (part1 == "test")
				{
					returnHandler = new TestApiHandler(uriW, sh);
				}

				else if (part1 == "folders")
				{
					returnHandler = new FoldersApiHandler(uriW, sh);
				}

				else if (part1 == "jukebox")
				{
					returnHandler = new JukeboxApiHandler(uriW, sh);
				}

				else if (part1 == "artists")
				{
					returnHandler = new ArtistsApiHandler(uriW, sh);
				}

				else if (part1 == "albums")
				{
					returnHandler = new AlbumsApiHandler(uriW, sh);
				}

				else if (part1 == "songs")
				{
					returnHandler = new SongsApiHandler(uriW, sh);
				}

				else if (part1 == "stream")
				{
					returnHandler = new StreamApiHandler(uriW, sh);
				}

				else if (part1 == "cover")
				{
					returnHandler = new CoverArtApiHandler(uriW, sh);
				}

				else if (part1 == "status")
				{
					returnHandler = new StatusApiHandler(uriW, sh);
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
