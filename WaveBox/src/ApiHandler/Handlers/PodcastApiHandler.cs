using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using WaveBox.ApiHandler;
using WaveBox.Http;
using WaveBox.PodcastManagement;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
    class PodcastApiHandler : IApiHandler
    {
        private HttpProcessor Processor { get; set; }
        private UriWrapper Uri { get; set; }

        public PodcastApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
        {
            Processor = processor;
            Uri = uri;
        }

        public void Process()
        {
            var listToReturn = new List<Podcast>();

            if (Uri.UriPart(2) == null)
            {
                if(Uri.Parameters.ContainsKey("action"))
                {
                    string action = null;
                    Uri.Parameters.TryGetValue("action", out action);

                    if(action == null)
                    {
                        Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Parameter 'action' contained an invalid value", null)));
                    }

                    else
                    {
                        if(action == "add")
                        {
                            if(Uri.Parameters.ContainsKey("podcastUrl") && Uri.Parameters.ContainsKey("podcastKeepCap"))
                            {
                                string podcastUrl = null;
                                string keepCapTemp = null;
                                int keepCap = 0;
                                Uri.Parameters.TryGetValue("podcastUrl", out podcastUrl);
                                Uri.Parameters.TryGetValue("podcastKeepCap", out keepCapTemp);

                                if(podcastUrl == null)
                                {
                                    Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Parameter 'podcastUrl' contained an invalid value", null)));
                                }

                                else if(keepCapTemp == null)
                                {
                                    Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Parameter 'podcastKeepCap' contained an invalid value", null)));
                                }
                                else 
                                {
                                    if(!Int32.TryParse(keepCapTemp, out keepCap))
                                    {
                                        Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Parameter 'podcastKeepCap' contained an invalid value", null)));
                                    }
                                    podcastUrl = System.Web.HttpUtility.UrlDecode(podcastUrl);
                                    var pod = new Podcast(podcastUrl, keepCap);
                                    pod.DownloadNewEpisodes();
                                    Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse(null, null)));
                                    return;
                                }
                            }
                            else Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Missing parameter for action 'add'", null)));
                        }
                    }
                }

                listToReturn = Podcast.ListOfStoredPodcasts();
                Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse(null, listToReturn)));
                return;
            }
            else
            {
                int podcastId = 0;
                Int32.TryParse(Uri.UriPart(2), out podcastId);

                if(podcastId != 0)
                {
                    var thisPodcast = new Podcast(podcastId);
                    var epList = thisPodcast.ListOfStoredEpisodes();

                    Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse(null, thisPodcast, epList)));
                    return;
                }

                else
                {
                    Processor.WriteJson(JsonConvert.SerializeObject(new PodcastResponse("Invalid Podcast ID", null)));
                    return;
                }

            }
        }
    }

    class PodcastResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("podcasts")]
        public List<Podcast> Podcasts { get; set; }

        [JsonProperty("episodes")]
        public List<PodcastEpisode> Episodes { get; set; }

        public PodcastResponse(string error, List<Podcast> podcasts)
        {
            Error = error;
            Podcasts = podcasts;
            Episodes = null;
        }

        public PodcastResponse(string error, Podcast podcast, List<PodcastEpisode> episodes)
        {
            Error = error;
            Podcasts = new List<Podcast> { podcast };
            Episodes = episodes;
        }
    }
}