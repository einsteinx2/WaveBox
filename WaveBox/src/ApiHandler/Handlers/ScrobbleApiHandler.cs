using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.ApiHandler;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class ScrobbleApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		public ScrobbleApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		public void Process()
        {
            Lastfm lfm = new Lastfm(User);

            string action = null;
            string eve = null;
            string eveType = null;

            Uri.Parameters.TryGetValue("action", out action);
            Uri.Parameters.TryGetValue("event", out eve);
            Uri.Parameters.TryGetValue("eventType", out eveType);

            if (action == null || action == "auth")
            {
                if (!lfm.SessionAuthenticated)
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(null, lfm.AuthUrl)));
                }
                else
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMAlreadyAuthenticated")));
                }

                return;
            }


            if (!lfm.SessionAuthenticated)
            {
                Console.WriteLine("[SCROBBLE(1)] You must authenticate before you can scrobble.");

                Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNotAuthenticated", lfm.AuthUrl)));
                return;
            }

            List<LfmScrobbleData> scrobbles = new List<LfmScrobbleData>();


            LfmScrobbleType scrobbleType = Lastfm.ScrobbleTypeForString(action);
            if (scrobbleType == LfmScrobbleType.INVALID)
            {
                Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidScrobbleType")));
                return;
            }
            else if (scrobbleType == LfmScrobbleType.NOWPLAYING)
            {
                int id = Int32.MaxValue;
                if (!Uri.Parameters.ContainsKey("id"))
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoIdSpecifiedForNowPlaying")));
                    return;
                }

                bool getIdSuccess = false;
                string idTemp;

                if(!Uri.Parameters.TryGetValue("id", out idTemp))
                   getIdSuccess = false;
                else if (!Int32.TryParse(idTemp, out id))
                    getIdSuccess = false;
                else getIdSuccess = true;

                if(getIdSuccess == false)
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidIdSpecifiedForNowPlaying")));
                    return;
                }

                scrobbles.Add(new LfmScrobbleData(id, null));
                lfm.Scrobble(scrobbles, scrobbleType);
            }
            else
            {                
                if(eve == null)
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMNoEventSpecifiedForScrobble")));
                    return;
                }

                string[] input = eve.Split(',');

                if(input.Length % 2 != 0)
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse("LFMInvalidInput")));
                    return;
                }

                int i = 0;
                while(i < input.Length)
                {
                    scrobbles.Add(new LfmScrobbleData(int.Parse(input[i]), long.Parse(input[i + 1])));
                    i = i + 2;
                }
            }

            string result = lfm.Scrobble(scrobbles, scrobbleType);
            dynamic resp;

            if(result != null)
            {
                try
                {
                    resp = JsonConvert.DeserializeObject(result);
                }
                catch(Exception e)
                {
                    Console.WriteLine("[SCROBBLE(3)] ERROR: " + e);
                }
            }

            else return;

            if(resp.nowplaying != null || (resp.scrobbles != null))
            {
                try
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse()));
                }
                catch(Exception e)
                {
                    Console.WriteLine("[SCROBBLE(4)] ERROR: " + e);
                }
                return;
            }

            else if (resp.error != null)
            {
                try
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new ScrobbleResponse(string.Format("LFM{0}: {1}", resp.error, resp.message))));
                }
                catch(Exception e)
                {
                    Console.WriteLine("[SCROBBLE(5)] ERROR: " + e);
                }
                return;
            }
		}

		private class ScrobbleResponse
	    {
	        [JsonProperty("error")]
	        public string Error { get; set; }

	        [JsonProperty("authUrl")]
	        public string AuthUrl { get; set; }

            public ScrobbleResponse()
            {
                Error = null;
                AuthUrl = null;
            }

	        public ScrobbleResponse(string error)
	        {
	            Error = error;
	            AuthUrl = null;
	        }

	        public ScrobbleResponse(string error, string authUrl)
	        {
	            Error = error;
	            AuthUrl = authUrl;
	        }
	    }
	}
}
