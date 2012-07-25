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

				if(!lfm.SessionAuthenticated)
				{
					Console.WriteLine ("[SCROBBLE(1)] You must authenticate before you can scrobble.");

					try
					{
	                    WaveBoxHttpServer.sendJson(Processor, JsonConvert.SerializeObject(new ScrobbleResponse("LFMNotAuthenticated", false, lfm.AuthUrl)));
					}
					catch(Exception e)
					{
						Console.WriteLine("[SCROBBLE(2)] ERROR: " + e.ToString());
					}
                    return;
				}

				else if(Uri.UriPart(2) != null && Uri.Parameters.ContainsKey("insert"))
				{
                    string insertString = "";
                    Uri.Parameters.TryGetValue("insert", out insertString);
                    bool insertScrobble;

                    if(insertString == "0") insertScrobble = false;
                    else insertScrobble = true;
					
                    string result = lfm.Scrobble(Int32.Parse(Uri.UriPart(2)), insertScrobble);
                    dynamic resp;

                    if(result != null)
                    {
						try
						{
						    resp = JsonConvert.DeserializeObject(result);
						}
						catch(Exception e)
						{
							Console.WriteLine("[SCROBBLE(3)] ERROR: " + e.ToString());
						}
                    }

                    else return;

                    if(resp.nowplaying != null || (resp.scrobbles != null))
                    {
						try
						{
	                        WaveBoxHttpServer.sendJson(Processor, JsonConvert.SerializeObject(new ScrobbleResponse(null, true)));
						}
						catch(Exception e)
						{
							Console.WriteLine("[SCROBBLE(4)] ERROR: " + e.ToString());
						}
                        return;
                    }

                    else
                    {
						try
						{
	                        WaveBoxHttpServer.sendJson(Processor, JsonConvert.SerializeObject(new ScrobbleResponse(string.Format("LFM{0}", resp.error.code), false)));
						}
						catch(Exception e)
						{
							Console.WriteLine("[SCROBBLE(5)] ERROR: " + e.ToString());
						}
                        return;
                    }

				}
			}
		}
	}

    class ScrobbleResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("authUrl")]
        public string AuthUrl { get; set; }

        public ScrobbleResponse(string error, bool success)
        {
            Error = error;
            Success = success;
            AuthUrl = null;
        }

        public ScrobbleResponse(string error, bool success, string authUrl)
        {
            Error = error;
            Success = success;
            AuthUrl = authUrl;
        }
    }
}
