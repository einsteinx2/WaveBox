using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.HttpServer;

namespace WaveBox.ApiHandler.Handlers
{
	class ScrobbleApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private int UserId { get; set; }

		public ScrobbleApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
			UserId = userId;
		}

		public void Process()
		{
			if (Uri.UriPart(1) == "scrobble")
			{
				var lfm = new Lastfm(UserId);

				if(lfm.AuthUrl != null)
				{
					Console.WriteLine ("[SCROBBLE] You must authenticate before you can scrobble.");
				}

				else if(Uri.UriPart(2) != null && Uri.Parameters.ContainsKey("insert"))
				{
                    string insertString = "";
                    Uri.Parameters.TryGetValue("insert", out insertString);
                    bool insertScrobble;

                    if(insertString == "0") insertScrobble = false;
                    else insertScrobble = true;
					
					lfm.Scrobble(Int32.Parse(Uri.UriPart(2)), insertScrobble);
				}
			}
		}
	}
}

